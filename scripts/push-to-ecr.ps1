# PowerShell script to build and push Docker image to AWS ECR
# Usage: .\scripts\push-to-ecr.ps1 -AwsAccountId <account-id> -AwsRegion <region> -RepositoryName <repo-name>

param(
    [Parameter(Mandatory=$true)]
    [string]$AwsAccountId,
    
    [Parameter(Mandatory=$true)]
    [string]$AwsRegion,
    
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "event-rsvp-api",
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

$ErrorActionPreference = "Stop"

Write-Host "Building and pushing Docker image to ECR..." -ForegroundColor Green
Write-Host "Account ID: $AwsAccountId" -ForegroundColor Cyan
Write-Host "Region: $AwsRegion" -ForegroundColor Cyan
Write-Host "Repository: $RepositoryName" -ForegroundColor Cyan
Write-Host "Tag: $ImageTag" -ForegroundColor Cyan
Write-Host ""

# ECR registry URL
$ecrRegistry = "$AwsAccountId.dkr.ecr.$AwsRegion.amazonaws.com"
$imageUri = "$ecrRegistry/$RepositoryName`:$ImageTag"

Write-Host "Step 1: Authenticating with ECR..." -ForegroundColor Yellow
aws ecr get-login-password --region $AwsRegion | docker login --username AWS --password-stdin $ecrRegistry

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to authenticate with ECR. Please check your AWS credentials." -ForegroundColor Red
    exit 1
}

Write-Host "Step 2: Building Docker image..." -ForegroundColor Yellow
docker build -t $imageUri .

if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker build failed." -ForegroundColor Red
    exit 1
}

Write-Host "Step 3: Tagging image as latest..." -ForegroundColor Yellow
docker tag $imageUri "$ecrRegistry/$RepositoryName`:latest"

Write-Host "Step 4: Pushing image to ECR..." -ForegroundColor Yellow
docker push $imageUri

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to push image to ECR." -ForegroundColor Red
    exit 1
}

docker push "$ecrRegistry/$RepositoryName`:latest"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to push latest tag to ECR." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Successfully pushed image to ECR!" -ForegroundColor Green
Write-Host "Image URI: $imageUri" -ForegroundColor Cyan
Write-Host "Latest URI: $ecrRegistry/$RepositoryName`:latest" -ForegroundColor Cyan

