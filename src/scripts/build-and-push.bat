@echo off
REM ============================================================================
REM Build and Push ALL Docker Images to Docker Hub
REM ============================================================================
REM This script builds all microservice images and pushes them to Docker Hub
REM Make sure you're logged in to Docker Hub first: docker login
REM ============================================================================

echo ============================================================================
echo Building and Pushing Docker Images to Docker Hub (vimis222)
echo ============================================================================
echo.

REM Set Docker Hub username
set DOCKER_USERNAME=vimis222

REM Save current directory and change to project root
pushd %~dp0\..\..

echo [1/4] Building and pushing API Gateway...
echo ----------------------------------------------------------------------------
docker build -t %DOCKER_USERNAME%/api-gateway:latest ./src/API-Gateway
if errorlevel 1 (
    echo ERROR: Failed to build API Gateway
    pause
    exit /b 1
)
docker push %DOCKER_USERNAME%/api-gateway:latest
if errorlevel 1 (
    echo ERROR: Failed to push API Gateway. Did you run 'docker login'?
    pause
    exit /b 1
)
echo ✓ API Gateway built and pushed successfully
echo.

echo [2/4] Building and pushing Orchestrator...
echo ----------------------------------------------------------------------------
docker build -t %DOCKER_USERNAME%/orchestrator:latest ./src/Orchestrator
if errorlevel 1 (
    echo ERROR: Failed to build Orchestrator
    pause
    exit /b 1
)
docker push %DOCKER_USERNAME%/orchestrator:latest
if errorlevel 1 (
    echo ERROR: Failed to push Orchestrator
    pause
    exit /b 1
)
echo ✓ Orchestrator built and pushed successfully
echo.

echo [3/4] Building and pushing External Service...
echo ----------------------------------------------------------------------------
docker build -t %DOCKER_USERNAME%/external-service:latest ./src/External-Service
if errorlevel 1 (
    echo ERROR: Failed to build External Service
    pause
    exit /b 1
)
docker push %DOCKER_USERNAME%/external-service:latest
if errorlevel 1 (
    echo ERROR: Failed to push External Service
    pause
    exit /b 1
)
echo ✓ External Service built and pushed successfully
echo.

echo [4/4] Building and pushing Book Scheduler...
echo ----------------------------------------------------------------------------
docker build -t %DOCKER_USERNAME%/book-scheduler:latest ./src/BookScheduler_MQTT
if errorlevel 1 (
    echo ERROR: Failed to build Book Scheduler
    pause
    exit /b 1
)
docker push %DOCKER_USERNAME%/book-scheduler:latest
if errorlevel 1 (
    echo ERROR: Failed to push Book Scheduler
    pause
    exit /b 1
)
echo ✓ Book Scheduler built and pushed successfully
echo.

echo ============================================================================
echo ✓ ALL IMAGES BUILT AND PUSHED SUCCESSFULLY!
echo ============================================================================
echo.
echo Your images are now available on Docker Hub:
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/api-gateway
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/orchestrator
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/external-service
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/book-scheduler
echo.
echo Marco and Kasper can now pull your images with:
echo   docker-compose pull
echo.

REM Return to original directory
popd

pause
