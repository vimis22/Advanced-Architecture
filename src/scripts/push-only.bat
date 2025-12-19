@echo off
REM ============================================================================
REM Push Existing Docker Images to Docker Hub (No Rebuild)
REM ============================================================================
REM This script pushes already-built images to Docker Hub without rebuilding
REM Use this when you've already built images locally and just want to push
REM Make sure you're logged in to Docker Hub first: docker login
REM ============================================================================

echo ============================================================================
echo Pushing Existing Docker Images to Docker Hub (vimis222)
echo ============================================================================
echo.

REM Set Docker Hub username
set DOCKER_USERNAME=vimis222

echo [1/4] Pushing API Gateway...
docker push %DOCKER_USERNAME%/api-gateway:latest
if errorlevel 1 (
    echo ERROR: Failed to push API Gateway
    pause
    exit /b 1
)
echo ✓ API Gateway pushed
echo.

echo [2/4] Pushing Orchestrator...
docker push %DOCKER_USERNAME%/orchestrator:latest
if errorlevel 1 (
    echo ERROR: Failed to push Orchestrator
    pause
    exit /b 1
)
echo ✓ Orchestrator pushed
echo.

echo [3/4] Pushing External Service...
docker push %DOCKER_USERNAME%/external-service:latest
if errorlevel 1 (
    echo ERROR: Failed to push External Service
    pause
    exit /b 1
)
echo ✓ External Service pushed
echo.

echo [4/4] Pushing Book Scheduler...
docker push %DOCKER_USERNAME%/book-scheduler:latest
if errorlevel 1 (
    echo ERROR: Failed to push Book Scheduler
    pause
    exit /b 1
)
echo ✓ Book Scheduler pushed
echo.

echo ============================================================================
echo ✓ ALL IMAGES PUSHED SUCCESSFULLY!
echo ============================================================================
echo.
pause
