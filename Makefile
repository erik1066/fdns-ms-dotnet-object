docker-build:
	docker build \
		-t fdns-ms-dotnet-object \
		--rm \
		--force-rm=true \
		--build-arg SYSTEM_NAME=fdns \
		--build-arg OBJECT_PORT=9090 \
		--build-arg OBJECT_MONGO_CONNECTION_STRING=mongodb://mongo:27017 \
		--build-arg OBJECT_MONGO_USE_SSL=false \
		--build-arg OBJECT_FLUENTD_HOST=fluentd \
		--build-arg OBJECT_FLUENTD_PORT=24224 \
		--build-arg OBJECT_IMMUTABLE= \
		--build-arg OBJECT_HEALTH_CHECK_DATABASE_NAME=_healthcheckdatabase_ \
		--build-arg OBJECT_HEALTH_CHECK_COLLECTION_NAME=_healthcheckcollection_ \
		--build-arg OAUTH2_ACCESS_TOKEN_URI= \
		--build-arg OAUTH2_READINESS_CHECK_URI= \
		--build-arg OAUTH2_CLIENT_ID= \
		--build-arg OAUTH2_CLIENT_SECRET= \
		.

docker-run: docker-start
docker-start:
	docker-compose up --detach
	docker run -d \
		-p 9090:9090 \
		--network=fdns-ms-dotnet-object_default  \
		--name=fdns-ms-dotnet-object_main \
		fdns-ms-dotnet-object

docker-stop:
	docker stop fdns-ms-dotnet-object_main || true
	docker rm fdns-ms-dotnet-object_main || true
	docker-compose down --volume

docker-restart:
	make docker-stop 2>/dev/null || true
	make docker-start


# *************************
# *        testing        *
# *************************

# Unit tests
run-unit-tests:
	docker build \
		-t fdns-ms-dotnet-object-tests \
		-f tests/unit/Dockerfile.test \
		--rm \
		--force-rm=true \
		.
	docker rmi fdns-ms-dotnet-object-tests

# Integration tests
run-integration-tests:
	docker-compose --file tests/integration/docker-compose.yml up --detach
	sleep 7
	dotnet clean
	dotnet test tests/integration/Foundation.ObjectService.IntegrationTests.csproj || true
	docker-compose --file tests/integration/docker-compose.yml down --volume

# Performance tests
run-performance-tests:
	docker-compose --file tests/performance/docker-compose.yml up --detach
	printf 'Wait for Object service\n'
	until `curl --output /dev/null --silent --fail --connect-timeout 80 http://localhost:9090/health/ready`; do printf '.'; sleep 1; done
	sleep 1
	printf '\n'
	ab -p tests/performance/resources/001.json -T application/json -c 2 -n 1000 http://localhost:9090/api/1.0/bookstore/books
	printf '\n'
	docker-compose --file tests/performance/docker-compose.yml down --volume

# Security tests
run-security-tests:
	mkdir ./tests/security/resources || true
	docker-compose --file tests/security/docker-compose.yml up --detach
	printf 'Wait for Hydra\n'
	until `curl --output /dev/null --silent --fail --connect-timeout 80 http://localhost:4445/health/ready`; do printf '.'; sleep 1; done
	sleep 2
	docker exec -it security_hydra_1 \
		hydra clients create \
		--endpoint http://localhost:4445 \
		--scope "fdns.object.bookstore.*.* fdns.object.bookstore.books.read fdns.object.bookstore.books.insert fdns.object.bookstore.books.update fdns.object.bookstore.books.delete" \
		--id my-client \
		--secret secret \
		-g client_credentials
	docker exec -it `docker ps -f name=security_hydra_1 -q` \
		hydra token client \
		--endpoint http://localhost:4444 \
		--scope "fdns.object.bookstore.books.read fdns.object.bookstore.books.insert" \
		--client-id my-client \
		--client-secret secret > ./tests/security/resources/token-read-insert 2>&1
	docker exec -it `docker ps -f name=security_hydra_1 -q` \
		hydra token client \
		--endpoint http://localhost:4444 \
		--scope "fdns.object.bookstore.books.update fdns.object.bookstore.books.delete" \
		--client-id my-client \
		--client-secret secret > ./tests/security/resources/token-update-delete 2>&1
	docker exec -it `docker ps -f name=security_hydra_1 -q` \
		hydra token client \
		--endpoint http://localhost:4444 \
		--scope "fdns.object.bookstore.*.*" \
		--client-id my-client \
		--client-secret secret > ./tests/security/resources/token-bookstore-all-all 2>&1
	dotnet clean
	dotnet test tests/security/Foundation.ObjectService.SecurityTests.csproj || true
	docker-compose --file tests/security/docker-compose.yml down --volume

# SonarQube
sonar-up:
	docker pull sonarqube
	docker run -d --name sonarqube -p 9000:9000 -p 9092:9092 sonarqube || true

sonar-run: sonar-start
sonar-start:
	printf 'Wait for sonarqube\n'
	until `curl --output /dev/null --silent --head --fail --connect-timeout 80 http://localhost:9000/api/server/version`; do printf '.'; sleep 1; done
	sleep 5
	docker-compose up --detach
	dotnet tool install --global dotnet-sonarscanner || true
	dotnet sonarscanner begin /k:"fdns-ms-dotnet-object" || true
	dotnet test --collect:"Code Coverage"
	dotnet sonarscanner end || true
	docker-compose down --volume

sonar-stop: sonar-down
sonar-down:
	docker kill sonarqube || true
	docker rm sonarqube || true