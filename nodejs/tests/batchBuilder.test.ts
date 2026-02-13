import { describe, it, expect } from "vitest";
import { classifyFile, shouldExclude, countTokens, createBatches } from "../src/batchBuilder.js";
import { TechStack } from "../src/models.js";
import type { FilePatch, AppSettings } from "../src/models.js";
import { readFileSync } from "node:fs";
import { join } from "node:path";

// Load mock data
const mockDataPath = join(import.meta.dirname, "mockData.json");
const mockData = JSON.parse(readFileSync(mockDataPath, "utf-8"));

describe("batchBuilder", () => {
    describe("classifyFile", () => {
        it("should classify .NET files", () => {
            expect(classifyFile("src/Services/UserService.cs")).toBe(TechStack.Dotnet);
            expect(classifyFile("Project.csproj")).toBe(TechStack.Dotnet);
            expect(classifyFile("Solution.sln")).toBe(TechStack.Dotnet);
            expect(classifyFile("Component.razor")).toBe(TechStack.Dotnet);
            expect(classifyFile("View.cshtml")).toBe(TechStack.Dotnet);
        });

        it("should classify frontend files", () => {
            expect(classifyFile("src/App.tsx")).toBe(TechStack.Frontend);
            expect(classifyFile("src/App.jsx")).toBe(TechStack.Frontend);
            expect(classifyFile("src/index.js")).toBe(TechStack.Frontend);
            expect(classifyFile("src/styles.css")).toBe(TechStack.Frontend);
            expect(classifyFile("src/styles.scss")).toBe(TechStack.Frontend);
            expect(classifyFile("src/Component.vue")).toBe(TechStack.Frontend);
        });

        it("should classify Python files", () => {
            expect(classifyFile("src/main.py")).toBe(TechStack.Python);
            expect(classifyFile("src/types.pyi")).toBe(TechStack.Python);
        });

        it("should classify config files", () => {
            expect(classifyFile("package.json")).toBe(TechStack.Config);
            expect(classifyFile("config.yaml")).toBe(TechStack.Config);
            expect(classifyFile("settings.yml")).toBe(TechStack.Config);
        });

        it("should return undefined for unknown extensions", () => {
            expect(classifyFile("README.md")).toBeUndefined();
            expect(classifyFile("image.png")).toBeUndefined();
            expect(classifyFile("data.bin")).toBeUndefined();
        });
    });

    describe("shouldExclude", () => {
        it("should exclude lock files", () => {
            expect(shouldExclude("package-lock.json")).toBe(true);
            expect(shouldExclude("yarn.lock")).toBe(true);
            expect(shouldExclude("pnpm-lock.yaml")).toBe(true);
            expect(shouldExclude("Pipfile.lock")).toBe(true);
            expect(shouldExclude("poetry.lock")).toBe(true);
            expect(shouldExclude("packages.lock.json")).toBe(true);
        });

        it("should exclude minified files", () => {
            expect(shouldExclude("app.min.js")).toBe(true);
            expect(shouldExclude("styles.min.css")).toBe(true);
        });

        it("should exclude generated files", () => {
            expect(shouldExclude("Component.d.ts")).toBe(true);
            expect(shouldExclude("Model.g.cs")).toBe(true);
            expect(shouldExclude("Form.Designer.cs")).toBe(true);
            expect(shouldExclude("Entity.generated.cs")).toBe(true);
        });

        it("should not exclude regular files", () => {
            expect(shouldExclude("src/index.ts")).toBe(false);
            expect(shouldExclude("src/App.tsx")).toBe(false);
            expect(shouldExclude("package.json")).toBe(false);
        });
    });

    describe("countTokens", () => {
        it("should count tokens in text", () => {
            const text = "Hello, world! This is a test.";
            const count = countTokens(text);
            
            expect(count).toBeGreaterThan(0);
            expect(count).toBeLessThan(text.length); // Tokens are typically fewer than characters
        });

        it("should return 0 for empty string", () => {
            expect(countTokens("")).toBe(0);
        });

        it("should count tokens for code", () => {
            const code = "function test() { return 42; }";
            const count = countTokens(code);
            
            expect(count).toBeGreaterThan(0);
        });
    });

    describe("createBatches", () => {
        const mockSettings: AppSettings["copilot"] = {
            model: "gpt-4o",
            maxTokensPerBatch: 50000,
            overheadTokens: 5000,
            maxParallelBatches: 2,
            timeoutSeconds: 120,
        };

        it("should create batches from file patches", () => {
            const filePatches: FilePatch[] = mockData.sampleFilePatches;
            const result = createBatches(filePatches, mockSettings);

            expect(result.batches.length).toBeGreaterThan(0);
            expect(result.excludedFiles).toContain("package-lock.json");
        });

        it("should group files by tech stack", () => {
            const filePatches: FilePatch[] = mockData.sampleFilePatches;
            const result = createBatches(filePatches, mockSettings);

            const dotnetBatches = result.batches.filter(b => b.techStack === TechStack.Dotnet);
            const frontendBatches = result.batches.filter(b => b.techStack === TechStack.Frontend);

            expect(dotnetBatches.length).toBeGreaterThan(0);
            expect(frontendBatches.length).toBeGreaterThan(0);
        });

        it("should exclude files that should not be reviewed", () => {
            const filePatches: FilePatch[] = [
                ...mockData.sampleFilePatches,
                {
                    filePath: "dist/bundle.min.js",
                    newContent: "!function(){console.log('minified')}()",
                    patch: "@@ -0,0 +1 @@\n+!function(){console.log('minified')}()",
                    changeType: "add",
                    linesAdded: 1,
                    linesDeleted: 0,
                },
            ];
            
            const result = createBatches(filePatches, mockSettings);

            expect(result.excludedFiles).toContain("package-lock.json");
            expect(result.excludedFiles).toContain("dist/bundle.min.js");
        });

        it("should respect token limits per batch", () => {
            const smallSettings: AppSettings["copilot"] = {
                ...mockSettings,
                maxTokensPerBatch: 1000,
                overheadTokens: 100,
            };

            const largeContent = "a".repeat(10000);
            const filePatches: FilePatch[] = [
                {
                    filePath: "src/Large1.cs",
                    newContent: largeContent,
                    patch: `@@ -0,0 +1 @@\n+${largeContent}`,
                    changeType: "add",
                    linesAdded: 1,
                    linesDeleted: 0,
                },
                {
                    filePath: "src/Large2.cs",
                    newContent: largeContent,
                    patch: `@@ -0,0 +1 @@\n+${largeContent}`,
                    changeType: "add",
                    linesAdded: 1,
                    linesDeleted: 0,
                },
            ];

            const result = createBatches(filePatches, smallSettings);

            // Should create separate batches due to token limit
            expect(result.batches.length).toBeGreaterThanOrEqual(2);
        });

        it("should assign sequential batch numbers", () => {
            const filePatches: FilePatch[] = mockData.sampleFilePatches;
            const result = createBatches(filePatches, mockSettings);

            const batchNumbers = result.batches.map(b => b.batchNumber);
            
            // Check that batch numbers are sequential starting from 1
            expect(batchNumbers[0]).toBe(1);
            for (let i = 1; i < batchNumbers.length; i++) {
                expect(batchNumbers[i]).toBe(batchNumbers[i - 1] + 1);
            }
        });

        it("should include both content and diff in batches when extendReview is true", () => {
            const filePatches: FilePatch[] = mockData.sampleFilePatches;
            const result = createBatches(filePatches, mockSettings, true);

            const firstBatch = result.batches[0];
            const firstFile = Array.from(firstBatch.files.values())[0];

            expect(firstFile.content).toBeTruthy();
            expect(firstFile.diff).toBeTruthy();
        });

        it("should include only diff (no content) in batches when extendReview is false", () => {
            const filePatches: FilePatch[] = mockData.sampleFilePatches;
            const result = createBatches(filePatches, mockSettings, false);

            const firstBatch = result.batches[0];
            const firstFile = Array.from(firstBatch.files.values())[0];

            expect(firstFile.content).toBe("");
            expect(firstFile.diff).toBeTruthy();
        });
    });
});
