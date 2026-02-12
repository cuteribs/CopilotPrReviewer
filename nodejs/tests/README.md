# Test Suite

This directory contains the test suite for the Copilot PR Reviewer project.

## Test Structure

### Test Files

- **`reviewService.test.ts`** - Tests for the review service, including:
  - JSON extraction from Copilot responses
  - Parsing review findings
  - Handling different response formats
  - Mocking Copilot SDK sessions

- **`batchBuilder.test.ts`** - Tests for file batching logic:
  - File classification (Dotnet, Frontend, Python, Config)
  - File exclusion rules (lock files, minified files, generated files)
  - Token counting
  - Batch creation and token limits

- **`reporter.test.ts`** - Tests for output reporting:
  - Summary generation
  - Severity breakdown
  - Findings display

### Mock Data

**`mockData.json`** contains mock responses from the Copilot SDK's `assistant.message` events:

- `assistantMessages.dotnetReview` - Sample .NET code review findings
- `assistantMessages.frontendReview` - Sample frontend code review findings
- `assistantMessages.pythonReview` - Sample Python code review findings
- `assistantMessages.emptyReview` - Empty review response
- `assistantMessages.malformedResponse` - Invalid response format
- `assistantMessages.partialJson` - Partially formatted JSON
- `assistantMessages.noJsonBlock` - Raw JSON without code blocks
- `samplePrInfo` - Sample PR metadata
- `samplePrDetails` - Sample PR details
- `sampleFilePatches` - Sample file patches for testing

## Running Tests

### Run all tests once
```bash
npm test -- --run
```

### Run tests in watch mode (default)
```bash
npm test
```

### Run tests with UI
```bash
npm run test:ui
```

### Run tests with coverage
```bash
npm run test:coverage
```

### Run specific test file
```bash
npx vitest tests/reviewService.test.ts
```

## Test Coverage

The test suite covers:

✅ **Review Service**
- Copilot SDK session mocking
- Multiple response format handling
- Error handling (session errors, malformed responses)
- JSON extraction from various formats

✅ **Batch Builder**
- File classification by extension
- File exclusion rules
- Token counting and limits
- Batch creation and grouping by tech stack

✅ **Reporter**
- Summary output generation
- Severity breakdown
- Large finding sets

## Mocking Strategy

The tests use **Vitest's mocking capabilities** to mock the `@github/copilot-sdk`:

```typescript
vi.mock("@github/copilot-sdk", () => {
    return {
        CopilotClient: vi.fn().mockImplementation(() => {
            return {
                createSession: vi.fn(),
                stop: vi.fn().mockResolvedValue(undefined),
            };
        }),
    };
});
```

Individual tests then override `createSession` to return mock sessions with specific behaviors:

```typescript
const mockSession = {
    on: vi.fn((callback) => {
        callback({
            type: "assistant.message",
            data: { content: mockData.assistantMessages.dotnetReview },
        });
    }),
    sendAndWait: vi.fn().mockResolvedValue(undefined),
    destroy: vi.fn().mockResolvedValue(undefined),
};
```

## Adding New Tests

1. Create a new test file in the `tests/` directory with `.test.ts` extension
2. Import the necessary functions and types
3. Use `describe` for test suites and `it` for individual tests
4. Add mock data to `mockData.json` if needed
5. Run tests to verify they pass

Example:
```typescript
import { describe, it, expect } from "vitest";
import { myFunction } from "../src/myModule.js";

describe("myModule", () => {
    it("should do something", () => {
        const result = myFunction();
        expect(result).toBe(expected);
    });
});
```

## CI/CD Integration

Add these commands to your CI/CD pipeline:

```bash
npm install
npm test -- --run
npm run test:coverage
```

## Troubleshooting

### Tests fail with "Cannot find module"
Ensure you're using `.js` extensions in imports (TypeScript ESM requirement):
```typescript
import { something } from "../src/module.js"; // ✅ Correct
import { something } from "../src/module"; // ❌ Wrong
```

### Mock data not loading
Check that `mockData.json` is in the `tests/` directory and the path is correct:
```typescript
const mockDataPath = join(import.meta.dirname, "mockData.json");
```

### Tests timeout
Increase the timeout in vitest config or specific tests:
```typescript
it("long running test", async () => {
    // test code
}, 30000); // 30 second timeout
```
