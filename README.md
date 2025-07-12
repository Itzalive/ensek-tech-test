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
  - Account data cached per request to minimize DB hits.
- **Structured Logging**: Detailed, structured logs for easy debugging and monitoring in production.
- **Transactional Middleware**: Ensures atomicity of each request. (Although, may want to separate processing of large files into multiple transactions in future)
- **Integration Tests**: Uses `TestHost` and in-memory SQLite database for fast, isolated testing.
- **Layered Architecture**: Solution structure enforces internal implementations per layer, promoting encapsulation and maintainability.

## 🧪 Running Locally

Use Docker Compose (can be launched via Visual Studio for debugging):
- A SQL server container will be created with a database persisted to a volume.
- The DB Creator tool will be started which will migrate the database and seed the initial Account entries.

