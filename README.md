# 📊 Meter Readings API

A simple REST API for uploading meter readings. (Readme re-written using AI, without code access)

[![Automated Pipeline Status](https://github.com/ENSEK-Recruitment/PeteForrest/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ENSEK-Recruitment/PeteForrest/actions/workflows/dotnet.yml)

## Assumptions made
- Should be able to handle a large number of meter readings in the same request
- Likely to have additional validation rules in future

## 🚀 Features
- **CSV Upload**: Send CSV files in the request body to upload readings.
- **Streaming Input**: Uses an `InputFormatter` to convert CSV files with `IAsyncEnumerable` response for efficient streaming and extensibility.
- **High-Performance Parsing**: Utilizes the zero-allocation `Sep` parser for fast CSV processing.
- **Two-Stage Validation**: Splitting into these two stages means we do not load anything from the database which is not required.
  - **Parsing**: Validates format and non-database rules.
  - **Validation**: Applies database-dependent business rules.
- **Extensible Validation Rules**: Easily add new validation rules by implementing a simple interface.
- **Optimized DB Access**:
  - Reads processed in chunks of 2000 to reduce DB calls.
  - 'de-duplication' logic has been implemented as part of the 'must be newer' logic. If the reading is distinctly newer than the previous reading we know it must not be a duplicate
  - Account data decided not to cache between chunks. Although this increases DB hits, the chance of multiple readings for the same account in the same request is expected to be relatively low.
- **Structured Logging**: Detailed, structured logs for easy debugging and monitoring in production.
- **DB Transaction Abstraction**: Separates infrastrucure concerns from application implementation.
- **Integration Tests**: Uses `TestHost` and in-memory SQLite database for fast, isolated testing.
- **Layered Architecture**: Solution structure enforces internal implementations per layer, promoting encapsulation and maintainability.

## 🧪 Running Locally

Use Docker Compose (can be launched via Visual Studio for debugging):
- A SQL server container will be created with a database persisted to a volume.
- The DB Creator tool will be started which will migrate the database and seed the initial Account entries.

## Review Feedback

1. **Service implementation is already large**

    The implementation in the service class is already feeling larger than I'd like - I think I'd make this more specific to adding readings. Adding other 'service' methods in future would make the class even bigger than it already is.

2. **Validation Rule Structure**

    The method that iterates over all validation rules in IMeterReadingService could be abstracted into its own interface or service, such as I...Validator. Existing rules could then implement a more specific I...ValidatorRule interface for better separation of concerns and extensibility.

3. **Validation Rule Extensibility**

    While the validation framework is designed to be extensible, only one rule currently exists. This might be a case of YAGNI (You Aren’t Gonna Need It). Additionally, the use of an asynchronous interface for extensibility may be premature; although, a ValueTask should be as light weight as possible. A discussion with the product team could help clarify the expected future direction.

4. **Value Parsing Logic**

    The value parsing method is currently the only logic residing on the model. It was decided to be placed on the model for maximum reusability. It might be more appropriate to move this to the parser service to consolidate all parsing logic in one place.

5. **End-to-End Testing**

    Adding E2E tests using Docker Compose with a real SQL database is possible and would be beneficial. This would provide more accurate test coverage compared to the current SQLite-based tests.

6. **Database Creator Utility**

    There’s potential to evolve the database creator into a more general-purpose, verb-based command-line utility. This could improve flexibility and usability for future use cases.

