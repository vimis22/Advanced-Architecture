@echo off
REM ============================================================================
REM Build and Push Main Docker Images to Docker Hub
REM ============================================================================
REM This script builds the 3 main microservices Marco requested:
REM   - API Gateway
REM   - Orchestrator
REM   - External Service
REM BookScheduler is excluded due to compilation errors
REM ============================================================================

echo ============================================================================
echo Building and Pushing Main Services to Docker Hub (vimis222)
echo ============================================================================
echo.

REM Set Docker Hub username
set DOCKER_USERNAME=vimis222

REM Save current directory and change to project root
pushd %~dp0\..\..

echo [1/3] Building and pushing API Gateway...
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

echo [2/3] Building and pushing Orchestrator...
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

echo [3/3] Building and pushing External Service...
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

echo ============================================================================
echo ✓ ALL 3 MAIN SERVICES BUILT AND PUSHED SUCCESSFULLY!
echo ============================================================================
echo.
echo Your images are now available on Docker Hub:
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/api-gateway
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/orchestrator
echo   - https://hub.docker.com/r/%DOCKER_USERNAME%/external-service
echo.
echo Marco and Kasper can now pull your images with:
echo   docker-compose pull
echo.
echo NOTE: BookScheduler was NOT built due to compilation errors.
echo.

REM Return to original directory
popd

pause
