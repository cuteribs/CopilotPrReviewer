import { describe, it, expect } from "vitest";
import { printSummary } from "../src/reporter.js";
import type { ReviewSummary } from "../src/models.js";

describe("reporter", () => {
    describe("printSummary", () => {
        it("should print summary for PR with findings", () => {
            const summary: ReviewSummary = {
                prUrl: "https://dev.azure.com/org/project/_git/repo/pullrequest/123",
                prTitle: "Add authentication feature",
                totalFiles: 10,
                reviewedFiles: 8,
                excludedFiles: 2,
                totalBatches: 3,
                findings: [
                    {
                        severity: "Critical",
                        filePath: "src/auth/Service.cs",
                        lineNumber: 45,
                        description: "SQL injection vulnerability",
                        suggestion: "Use parameterized queries",
                    },
                    {
                        severity: "Major",
                        filePath: "src/auth/Controller.cs",
                        lineNumber: 23,
                        description: "Missing null check",
                        suggestion: "Add null check",
                    },
                    {
                        severity: "Minor",
                        filePath: "src/models/User.cs",
                        lineNumber: 12,
                        description: "Property should be readonly",
                        suggestion: "Use init accessor",
                    },
                ],
                commentsPosted: true,
                duration: 45000,
            };

            // Should not throw
            expect(() => printSummary(summary)).not.toThrow();
        });

        it("should print summary for PR with no findings", () => {
            const summary: ReviewSummary = {
                prUrl: "https://dev.azure.com/org/project/_git/repo/pullrequest/456",
                prTitle: "Fix typo in README",
                totalFiles: 1,
                reviewedFiles: 1,
                excludedFiles: 0,
                totalBatches: 1,
                findings: [],
                commentsPosted: false,
                duration: 5000,
            };

            // Should not throw
            expect(() => printSummary(summary)).not.toThrow();
        });

        it("should handle large number of findings", () => {
            const findings = Array.from({ length: 50 }, (_, i) => ({
                severity: i % 3 === 0 ? "Critical" : i % 3 === 1 ? "Major" : "Minor",
                filePath: `src/file${i}.cs`,
                lineNumber: i + 1,
                description: `Issue ${i}`,
                suggestion: `Fix ${i}`,
            }));

            const summary: ReviewSummary = {
                prUrl: "https://dev.azure.com/org/project/_git/repo/pullrequest/789",
                prTitle: "Large refactoring",
                totalFiles: 50,
                reviewedFiles: 50,
                excludedFiles: 0,
                totalBatches: 10,
                findings,
                commentsPosted: false,
                duration: 120000,
            };

            // Should not throw
            expect(() => printSummary(summary)).not.toThrow();
        });
    });
});
