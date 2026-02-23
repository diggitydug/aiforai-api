variable "aws_region" {
  description = "AWS region for all resources"
  type        = string
  default     = "us-east-1"
}

variable "project_name" {
  description = "Project prefix used for resource naming"
  type        = string
  default     = "aiforai-api"
}

variable "environment" {
  description = "Environment name, e.g. dev/staging/prod"
  type        = string
  default     = "dev"
}

variable "lambda_zip_path" {
  description = "Path to the pre-built Lambda deployment zip file"
  type        = string
}

variable "lambda_handler" {
  description = "Lambda handler entrypoint for the deployed assembly"
  type        = string
  default     = "AiForAi.Api"
}

variable "lambda_runtime" {
  description = "Lambda runtime identifier"
  type        = string
  default     = "dotnet10"
}

variable "lambda_timeout_seconds" {
  description = "Lambda function timeout"
  type        = number
  default     = 30
}

variable "lambda_memory_mb" {
  description = "Lambda memory size"
  type        = number
  default     = 512
}

variable "current_tos_version" {
  description = "Optional explicit TOS version override. Leave empty to derive from tos.txt Version line (or content hash fallback)."
  type        = string
  default     = ""
}

variable "api_domain_name" {
  description = "Optional custom domain for API Gateway HTTP API (e.g. api.aiforai.dev)."
  type        = string
  default     = ""
}

variable "acm_certificate_arn" {
  description = "ACM certificate ARN for the custom API domain. Required when api_domain_name is set."
  type        = string
  default     = ""
}

variable "route53_zone_id" {
  description = "Optional Route53 hosted zone ID where alias record for api_domain_name should be created."
  type        = string
  default     = ""
}

variable "tags" {
  description = "Common tags for all resources"
  type        = map(string)
  default     = {}
}
