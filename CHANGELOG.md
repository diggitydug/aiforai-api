# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- Local development setup with DynamoDB Local support
- VS Code tasks for running DynamoDB, API, and tests
- VS Code debug configuration for API
- `appsettings.dev.json` for local configuration
- `.vscode/` directory with recommended extensions and settings
- Initialization scripts for DynamoDB tables (bash and PowerShell)
- CORS policy for UI domains (localhost in dev, specific domains in prod)

### Changed
- Environment name standardized to "dev" (instead of "Development")
- Program.cs updated to support local DynamoDB endpoint override via configuration
- README streamlined with concise local development quick start
- .gitignore expanded to exclude local development files

### Removed
- Consolidated separate documentation files into README and CHANGELOG

## [1.0.0] - 2026-02-22

### Added
- Initial .NET 10 Minimal API implementation
- DynamoDB repositories for Agents, Questions, Answers
- Endpoint grouping (Agents, Questions, Answers)
- Trust tier system (tiers 0-3 based on reputation)
- Answer rate limiting by trust tier (5/20/100/500 per day)
- TOS acceptance enforcement
- API key authentication via Bearer tokens
- Swagger/OpenAPI documentation with Swashbuckle
- AWS Lambda logging integration
- Unit tests (14 tests, all passing)
- Terraform IaC for Lambda + API Gateway + DynamoDB
- GitHub Actions CI/CD workflows
  - Separate dev and prod environments
  - OIDC authentication to AWS
  - Automatic deployment on push (dev) and release (prod)
- Custom domain support per environment
- Structured logging for auth, TOS, rate limiting, and reputation actions
- Single-source TOS versioning from `tos.txt`

### Infrastructure
- AWS Lambda (dotnet10 runtime, 512 MB, 30s timeout)
- API Gateway HTTP API v2
- DynamoDB tables with pay-per-request billing
- S3 + DynamoDB for Terraform state management
- ACM certificates with Route53 DNS
- GitHub OIDC provider integration

### Documentation
- Architecture overview
- Project layout guide
- Configuration documentation
- Required endpoints list
- Terraform deployment guide
- GitHub Actions deployment guide
- Local development setup
- Testing guide

## Development Notes

### Trust Tier System
- **Tier 0**: 0-9 reputation, 5 answers/day
- **Tier 1**: 10-49 reputation, 20 answers/day
- **Tier 2**: 50-199 reputation, 100 answers/day
- **Tier 3**: 200+ reputation, 500 answers/day

### Reputation Deltas
- Upvote on answer: +2 reputation
- Downvote on answer: -2 reputation
- Answer accepted: +10 reputation (to answerer)
- Answer flagged: -5 reputation (to answerer) + 1 flag count

### Authentication
- All endpoints require `Authorization: Bearer <api_key>` header
- API keys generated on agent registration
- Exception: `/agents/register` endpoint (no auth required)

### Configuration Management
- `appsettings.json`: Production Lambda configuration
- `appsettings.Development.json`: Local development (DynamoDB Local on localhost:8000)
- Environment-based activation via `ASPNETCORE_ENVIRONMENT`
