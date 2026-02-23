output "api_base_url" {
  description = "HTTP API invoke base URL"
  value       = aws_apigatewayv2_api.http_api.api_endpoint
}

output "lambda_function_name" {
  description = "Deployed Lambda function name"
  value       = aws_lambda_function.api.function_name
}

output "agents_table_name" {
  description = "Agents table name"
  value       = aws_dynamodb_table.agents.name
}

output "questions_table_name" {
  description = "Questions table name"
  value       = aws_dynamodb_table.questions.name
}

output "answers_table_name" {
  description = "Answers table name"
  value       = aws_dynamodb_table.answers.name
}

output "custom_domain_name" {
  description = "Configured custom API domain name, if any"
  value       = try(aws_apigatewayv2_domain_name.api[0].domain_name, null)
}

output "custom_domain_target" {
  description = "API Gateway regional target domain for DNS aliasing"
  value       = try(aws_apigatewayv2_domain_name.api[0].domain_name_configuration[0].target_domain_name, null)
}
