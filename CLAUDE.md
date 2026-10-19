
# Expense Tracker 

## Project Overview
Finance application for tracking expenses, budgets,
recurring payments and AI-powered receipt extraction.

## Technology Stack
- Backend: ASP.NET Core 10 / C#
- Frontend: React + TypeScript / Vite
- Database: Supabase PostgreSQL
- ORM: Entity Framework Core 10 / Npgsql
- AI Integration: Groq API
- Testing: xUnit
- Version Control: GitHub

## Project Structure
- expenseBackend/: .NET solution
- expenseFrontend/: React application
- docs/: requirements, architecture
- .claude/skills/: reusable agent workflows

## Architecture Rules
- Follow the existing layered architecture.
- Domain contains business entities and rules.
- Application contains use cases and abstractions.
- Infrastructure handles persistence and external services.
- API handles HTTP endpoints and request validation.
- Keep controllers thin.
- Avoid unnecessary abstractions and overengineering.

## Development Workflow
1. Understand the requirement.
2. Read relevant documentation and existing code.
3. Identify dependencies and potential risks.
4. Propose an implementation plan before major changes.
5. Wait for approval before implementing significant changes.
6. Implement incrementally with focused changes.
7. Add or update meaningful tests.
8. Run relevant builds and tests.
9. Review the final diff and report any remaining risks.

## Coding Standards
- Use async/await for database and HTTP I/O.
- Use decimal for financial calculations.
- Prefer dependency injection.
- Follow existing naming conventions.
- Use strongly typed DTOs.
- Handle null values and invalid input explicitly.
- Do not add packages without justification.
- Avoid unrelated refactoring.

## AI Integration Rules
- Never expose Groq API keys in frontend code.
- Validate model output before persisting it.
- Require user confirmation for AI-extracted expenses.
- Never invent missing financial information.
- Treat model responses as untrusted input.

## Testing Rules
- Write tests against expected requirements.
- Cover important edge cases.
- Do not modify tests simply to make them pass.
- Mock external AI services in unit tests.
- Report failing tests honestly.

## Security
- Never commit credentials, tokens or connection strings.
- Use environment variables or .NET User Secrets.
- Restrict database access by user.
- Do not expose sensitive financial information in logs.

## Documentation
- Architecture: docs/architecture.md
- Implementation: docs/implementations/
- Plans: docs/plans/
- Requirements: docs/requirements.md