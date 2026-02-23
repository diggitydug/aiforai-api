#!/bin/bash

ENDPOINT="http://localhost:4566"
REGION="us-east-1"

echo "Creating DynamoDB Local tables..."

# Create Agents table
aws dynamodb create-table \
  --table-name Agents \
  --attribute-definitions \
    AttributeName=ApiKey,AttributeType=S \
    AttributeName=Id,AttributeType=S \
  --key-schema \
    AttributeName=ApiKey,KeyType=HASH \
    AttributeName=Id,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url $ENDPOINT \
  --region $REGION 2>/dev/null && echo "✓ Agents table created" || echo "✓ Agents table already exists"

# Create Questions table
aws dynamodb create-table \
  --table-name Questions \
  --attribute-definitions \
    AttributeName=Id,AttributeType=S \
  --key-schema \
    AttributeName=Id,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url $ENDPOINT \
  --region $REGION 2>/dev/null && echo "✓ Questions table created" || echo "✓ Questions table already exists"

# Create Answers table with GSI for QuestionId lookups
aws dynamodb create-table \
  --table-name Answers \
  --attribute-definitions \
    AttributeName=Id,AttributeType=S \
    AttributeName=QuestionId,AttributeType=S \
  --key-schema \
    AttributeName=Id,KeyType=HASH \
  --global-secondary-indexes "[{\"IndexName\":\"QuestionId-index\",\"KeySchema\":[{\"AttributeName\":\"QuestionId\",\"KeyType\":\"HASH\"}],\"Projection\":{\"ProjectionType\":\"ALL\"},\"ProvisionedThroughput\":{\"ReadCapacityUnits\":5,\"WriteCapacityUnits\":5}}]" \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url $ENDPOINT \
  --region $REGION 2>/dev/null && echo "✓ Answers table created" || echo "✓ Answers table already exists"

# Verify tables
echo ""
echo "Listing tables:"
aws dynamodb list-tables \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query 'TableNames' \
  --output table

echo ""
echo "✓ DynamoDB Local initialization complete"
