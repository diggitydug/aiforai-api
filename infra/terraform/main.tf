provider "aws" {
  region = var.aws_region
}

locals {
  name_prefix = "${var.project_name}-${var.environment}"
  common_tags = merge(
    {
      Project     = var.project_name
      Environment = var.environment
      ManagedBy   = "terraform"
    },
    var.tags
  )
}

resource "aws_dynamodb_table" "agents" {
  name         = "${local.name_prefix}-Agents"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "agent_id"

  attribute {
    name = "agent_id"
    type = "S"
  }

  tags = local.common_tags
}

resource "aws_dynamodb_table" "questions" {
  name         = "${local.name_prefix}-Questions"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "question_id"

  attribute {
    name = "question_id"
    type = "S"
  }

  tags = local.common_tags
}

resource "aws_dynamodb_table" "answers" {
  name         = "${local.name_prefix}-Answers"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "answer_id"

  attribute {
    name = "answer_id"
    type = "S"
  }

  tags = local.common_tags
}

data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda_exec" {
  name               = "${local.name_prefix}-lambda-role"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume_role.json
  tags               = local.common_tags
}

resource "aws_iam_role_policy_attachment" "lambda_basic_exec" {
  role       = aws_iam_role.lambda_exec.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

data "aws_iam_policy_document" "dynamodb_access" {
  statement {
    sid = "DynamoDbCrudAccess"

    actions = [
      "dynamodb:GetItem",
      "dynamodb:PutItem",
      "dynamodb:UpdateItem",
      "dynamodb:DeleteItem",
      "dynamodb:Scan",
      "dynamodb:Query",
      "dynamodb:BatchGetItem",
      "dynamodb:BatchWriteItem"
    ]

    resources = [
      aws_dynamodb_table.agents.arn,
      aws_dynamodb_table.questions.arn,
      aws_dynamodb_table.answers.arn
    ]
  }
}

resource "aws_iam_policy" "lambda_dynamodb" {
  name   = "${local.name_prefix}-lambda-dynamodb-policy"
  policy = data.aws_iam_policy_document.dynamodb_access.json
  tags   = local.common_tags
}

resource "aws_iam_role_policy_attachment" "lambda_dynamodb_attach" {
  role       = aws_iam_role.lambda_exec.name
  policy_arn = aws_iam_policy.lambda_dynamodb.arn
}

resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${local.name_prefix}-api"
  retention_in_days = 14
  tags              = local.common_tags
}

resource "aws_lambda_function" "api" {
  function_name = "${local.name_prefix}-api"
  role          = aws_iam_role.lambda_exec.arn
  handler       = var.lambda_handler
  runtime       = var.lambda_runtime
  timeout       = var.lambda_timeout_seconds
  memory_size   = var.lambda_memory_mb

  filename         = var.lambda_zip_path
  source_code_hash = filebase64sha256(var.lambda_zip_path)

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = var.environment
      App__AgentsTable       = aws_dynamodb_table.agents.name
      App__QuestionsTable    = aws_dynamodb_table.questions.name
      App__AnswersTable      = aws_dynamodb_table.answers.name
      App__CurrentTosVersion = var.current_tos_version
      App__TosFilePath       = "tos.txt"
    }
  }

  depends_on = [
    aws_cloudwatch_log_group.lambda,
    aws_iam_role_policy_attachment.lambda_basic_exec,
    aws_iam_role_policy_attachment.lambda_dynamodb_attach
  ]

  tags = local.common_tags
}

resource "aws_apigatewayv2_api" "http_api" {
  name          = "${local.name_prefix}-http-api"
  protocol_type = "HTTP"
  tags          = local.common_tags
}

resource "aws_apigatewayv2_integration" "lambda_proxy" {
  api_id                 = aws_apigatewayv2_api.http_api.id
  integration_type       = "AWS_PROXY"
  integration_uri        = aws_lambda_function.api.invoke_arn
  integration_method     = "POST"
  payload_format_version = "2.0"
  timeout_milliseconds   = 29000
}

resource "aws_apigatewayv2_route" "default" {
  api_id    = aws_apigatewayv2_api.http_api.id
  route_key = "$default"
  target    = "integrations/${aws_apigatewayv2_integration.lambda_proxy.id}"
}

resource "aws_apigatewayv2_stage" "default" {
  api_id      = aws_apigatewayv2_api.http_api.id
  name        = "$default"
  auto_deploy = true
  tags        = local.common_tags
}

resource "aws_apigatewayv2_domain_name" "api" {
  count = var.api_domain_name != "" ? 1 : 0

  domain_name = var.api_domain_name

  domain_name_configuration {
    certificate_arn = var.acm_certificate_arn
    endpoint_type   = "REGIONAL"
    security_policy = "TLS_1_2"
  }

  lifecycle {
    precondition {
      condition     = var.api_domain_name == "" || var.acm_certificate_arn != ""
      error_message = "acm_certificate_arn must be set when api_domain_name is configured."
    }
  }

  tags = local.common_tags
}

resource "aws_apigatewayv2_api_mapping" "custom_domain_root" {
  count = var.api_domain_name != "" ? 1 : 0

  api_id      = aws_apigatewayv2_api.http_api.id
  domain_name = aws_apigatewayv2_domain_name.api[0].id
  stage       = aws_apigatewayv2_stage.default.id
}

resource "aws_route53_record" "api_alias_a" {
  count = var.api_domain_name != "" && var.route53_zone_id != "" ? 1 : 0

  zone_id = var.route53_zone_id
  name    = var.api_domain_name
  type    = "A"

  alias {
    name                   = aws_apigatewayv2_domain_name.api[0].domain_name_configuration[0].target_domain_name
    zone_id                = aws_apigatewayv2_domain_name.api[0].domain_name_configuration[0].hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_route53_record" "api_alias_aaaa" {
  count = var.api_domain_name != "" && var.route53_zone_id != "" ? 1 : 0

  zone_id = var.route53_zone_id
  name    = var.api_domain_name
  type    = "AAAA"

  alias {
    name                   = aws_apigatewayv2_domain_name.api[0].domain_name_configuration[0].target_domain_name
    zone_id                = aws_apigatewayv2_domain_name.api[0].domain_name_configuration[0].hosted_zone_id
    evaluate_target_health = false
  }
}

resource "aws_lambda_permission" "allow_api_gateway" {
  statement_id  = "AllowExecutionFromApiGateway"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.api.function_name
  principal     = "apigateway.amazonaws.com"
  source_arn    = "${aws_apigatewayv2_api.http_api.execution_arn}/*/*"
}
