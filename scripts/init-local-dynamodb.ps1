#!/usr/bin/env pwsh
# Initialize DynamoDB tables for local development (LocalStack)

$endpoint = "http://localhost:4566"
$region = "us-east-1"

if (-not $env:AWS_ACCESS_KEY_ID) { $env:AWS_ACCESS_KEY_ID = "test" }
if (-not $env:AWS_SECRET_ACCESS_KEY) { $env:AWS_SECRET_ACCESS_KEY = "test" }

function Test-TableExists {
    param([string]$TableName)

    aws dynamodb describe-table `
      --table-name $TableName `
      --endpoint-url $endpoint `
      --region $region *> $null

    return ($LASTEXITCODE -eq 0)
}

Write-Host "Creating DynamoDB tables..." -ForegroundColor Cyan

if (Test-TableExists "Agents") {
    Write-Host "Agents table already exists" -ForegroundColor Green
}
else {
    aws dynamodb create-table `
      --table-name Agents `
      --attribute-definitions `
        AttributeName=ApiKey,AttributeType=S `
        AttributeName=Id,AttributeType=S `
      --key-schema `
        AttributeName=ApiKey,KeyType=HASH `
        AttributeName=Id,KeyType=RANGE `
      --billing-mode PAY_PER_REQUEST `
      --endpoint-url $endpoint `
      --region $region *> $null

    if ($LASTEXITCODE -ne 0) { throw "Failed to create Agents table" }
    Write-Host "Agents table created" -ForegroundColor Green
}

if (Test-TableExists "Questions") {
    Write-Host "Questions table already exists" -ForegroundColor Green
}
else {
    aws dynamodb create-table `
      --table-name Questions `
      --attribute-definitions `
        AttributeName=Id,AttributeType=S `
      --key-schema `
        AttributeName=Id,KeyType=HASH `
      --billing-mode PAY_PER_REQUEST `
      --endpoint-url $endpoint `
      --region $region *> $null

    if ($LASTEXITCODE -ne 0) { throw "Failed to create Questions table" }
    Write-Host "Questions table created" -ForegroundColor Green
}

if (Test-TableExists "Answers") {
    Write-Host "Answers table already exists" -ForegroundColor Green
}
else {
    $gsiConfigPath = Join-Path $PSScriptRoot "answers-gsi.json"
    @'
[
  {
    "IndexName": "QuestionId-index",
    "KeySchema": [
      {
        "AttributeName": "QuestionId",
        "KeyType": "HASH"
      }
    ],
    "Projection": {
      "ProjectionType": "ALL"
    }
  }
]
'@ | Set-Content -Path $gsiConfigPath -Encoding Ascii

    aws dynamodb create-table `
      --table-name Answers `
      --attribute-definitions `
        AttributeName=Id,AttributeType=S `
        AttributeName=QuestionId,AttributeType=S `
      --key-schema `
        AttributeName=Id,KeyType=HASH `
      --global-secondary-indexes file://$gsiConfigPath `
      --billing-mode PAY_PER_REQUEST `
      --endpoint-url $endpoint `
      --region $region *> $null

    Remove-Item -Path $gsiConfigPath -ErrorAction SilentlyContinue

    if ($LASTEXITCODE -ne 0) { throw "Failed to create Answers table" }
    Write-Host "Answers table created" -ForegroundColor Green
}

Write-Host ""
Write-Host "Listing tables:" -ForegroundColor Cyan
aws dynamodb list-tables `
  --endpoint-url $endpoint `
  --region $region `
  --query 'TableNames' `
  --output table

if ($LASTEXITCODE -ne 0) { throw "Failed to list DynamoDB tables" }

Write-Host ""
Write-Host "Local DynamoDB initialization complete" -ForegroundColor Green
