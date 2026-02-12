import { describe, it, expect, vi, beforeEach } from "vitest";
import { reviewBatch, parseFindings, extractJson } from "../src/reviewService.js";
import type { ReviewBatch, AppSettings, ReviewFinding } from "../src/models.js";
import { TechStack } from "../src/models.js";
import { readFileSync } from "node:fs";
import { join } from "node:path";

// Load mock data
const mockDataPath = join(import.meta.dirname, "mockData.json");
const mockData = JSON.parse(readFileSync(mockDataPath, "utf-8"));

// Mock the @github/copilot-sdk
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

describe("reviewService", () => {
    let mockSettings: AppSettings;

    beforeEach(() => {
        mockSettings = {
            copilot: {
                model: "gpt-4o",
                maxTokensPerBatch: 50000,
                overheadTokens: 5000,
                maxParallelBatches: 2,
                timeoutSeconds: 120,
            },
            review: {
                postComments: false,
            },
            azureDevOps: {
                baseUrl: "https://dev.azure.com",
                authType: "bearer",
            },
        };
    });

    describe("extractJson", () => {
        it("should extract JSON from ```json code block", () => {
            const text = mockData.assistantMessages.dotnetReview;
            const json = extractJson(text);
            
            expect(json).toBeTruthy();
            expect(json).toContain('"severity"');
            expect(json).toContain('"filePath"');
        });

        it("should extract JSON from bare ``` code block", () => {
            const text = mockData.assistantMessages.partialJson;
            const json = extractJson(text);
            
            expect(json).toBeTruthy();
            expect(json).toContain('"severity"');
        });

        it("should extract raw JSON array", () => {
            const text = mockData.assistantMessages.noJsonBlock;
            const json = extractJson(text);
            
            expect(json).toBeTruthy();
            expect(json).toContain('"severity"');
        });

        it("should return null for malformed text", () => {
            const text = "This is just plain text without any JSON";
            const json = extractJson(text);
            
            expect(json).toBeNull();
        });
    });

    describe("parseFindings", () => {
        it("should parse valid .NET review findings", () => {
            const responseText = mockData.assistantMessages.dotnetReview;
            const findings = parseFindings(responseText);
            
            expect(findings).toHaveLength(3);
            expect(findings[0].severity).toBe("Critical");
            expect(findings[0].filePath).toBe("src/product/Repository.cs");
            expect(findings[0].lineNumber).toBe(43);
            expect(findings[0].description).toContain("SQL injection");
            expect(findings[0].suggestion).toBeTruthy();
        });

        it("should parse valid frontend review findings", () => {
            const responseText = mockData.assistantMessages.frontendReview;
            const findings = parseFindings(responseText);
            
            expect(findings).toHaveLength(2);
            expect(findings[0].severity).toBe("Critical");
            expect(findings[0].filePath).toBe("src/components/LoginForm.tsx");
            expect(findings[0].description).toContain("Password is logged");
        });

        it("should parse valid Python review findings", () => {
            const responseText = mockData.assistantMessages.pythonReview;
            const findings = parseFindings(responseText);
            
            expect(findings).toHaveLength(1);
            expect(findings[0].severity).toBe("Major");
            expect(findings[0].filePath).toBe("src/services/data_processor.py");
        });

        it("should return empty array for empty review", () => {
            const responseText = mockData.assistantMessages.emptyReview;
            const findings = parseFindings(responseText);
            
            expect(findings).toEqual([]);
        });

        it("should return empty array for malformed response", () => {
            const responseText = mockData.assistantMessages.malformedResponse;
            const findings = parseFindings(responseText);
            
            expect(findings).toEqual([]);
        });

        it("should return empty array for empty string", () => {
            const findings = parseFindings("");
            expect(findings).toEqual([]);
        });
    });

    describe("reviewBatch", () => {
        it("should review a batch and return findings", async () => {
            // Create mock session and client
            const mockSession = {
                on: vi.fn((callback) => {
                    // Simulate assistant.message event
                    callback({
                        type: "assistant.message",
                        data: { content: mockData.assistantMessages.dotnetReview },
                    });
                }),
                sendAndWait: vi.fn().mockResolvedValue(undefined),
                destroy: vi.fn().mockResolvedValue(undefined),
            };

            const mockClient = {
                createSession: vi.fn().mockResolvedValue(mockSession),
                stop: vi.fn().mockResolvedValue(undefined),
            };

            // Mock the CopilotClient constructor
            const { CopilotClient } = await import("@github/copilot-sdk");
            vi.mocked(CopilotClient).mockImplementation(() => mockClient as any);

            const batch: ReviewBatch = {
                batchNumber: 1,
                techStack: TechStack.Dotnet,
                totalTokens: 5000,
                files: new Map([
                    [
                        "src/product/Repository.cs",
                        {
                            content: "public class Repository { }",
                            diff: "@@ -0,0 +1 @@\n+public class Repository { }",
                        },
                    ],
                ]),
            };

            const findings = await reviewBatch(batch, mockSettings);

            expect(findings).toHaveLength(3);
            expect(findings[0].severity).toBe("Critical");
            expect(mockSession.sendAndWait).toHaveBeenCalled();
            expect(mockSession.destroy).toHaveBeenCalled();
        });

        it("should handle session errors", async () => {
            const mockSession = {
                on: vi.fn((callback) => {
                    callback({
                        type: "session.error",
                        data: { message: "API rate limit exceeded" },
                    });
                }),
                sendAndWait: vi.fn().mockResolvedValue(undefined),
                destroy: vi.fn().mockResolvedValue(undefined),
            };

            const mockClient = {
                createSession: vi.fn().mockResolvedValue(mockSession),
                stop: vi.fn().mockResolvedValue(undefined),
            };

            const { CopilotClient } = await import("@github/copilot-sdk");
            vi.mocked(CopilotClient).mockImplementation(() => mockClient as any);

            const batch: ReviewBatch = {
                batchNumber: 1,
                techStack: TechStack.Dotnet,
                totalTokens: 5000,
                files: new Map([
                    ["src/test.cs", { content: "test", diff: "test" }],
                ]),
            };

            await expect(reviewBatch(batch, mockSettings)).rejects.toThrow(
                "API rate limit exceeded"
            );
        });

        it("should handle empty response from assistant", async () => {
            const mockSession = {
                on: vi.fn((callback) => {
                    callback({
                        type: "assistant.message",
                        data: { content: mockData.assistantMessages.emptyReview },
                    });
                }),
                sendAndWait: vi.fn().mockResolvedValue(undefined),
                destroy: vi.fn().mockResolvedValue(undefined),
            };

            const mockClient = {
                createSession: vi.fn().mockResolvedValue(mockSession),
                stop: vi.fn().mockResolvedValue(undefined),
            };

            const { CopilotClient } = await import("@github/copilot-sdk");
            vi.mocked(CopilotClient).mockImplementation(() => mockClient as any);

            const batch: ReviewBatch = {
                batchNumber: 1,
                techStack: TechStack.Frontend,
                totalTokens: 3000,
                files: new Map([
                    ["src/test.tsx", { content: "test", diff: "test" }],
                ]),
            };

            const findings = await reviewBatch(batch, mockSettings);

            expect(findings).toEqual([]);
        });
    });
});
