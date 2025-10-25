// lib/contracts.ts
import { z } from "zod";

/** Raw server question as returned by /sessions/{id}/next */
export const ApiQuestionZ = z.object({
  id: z.number(),
  item_id: z.string(),                // server uses snake_case ItemId field
  text_ar: z.string(),               // server uses text_ar field
  type: z.string(),
  dimension_tags: z.string().optional().nullable(), // server uses dimension_tags
  difficulty: z.number().optional().nullable(),
  time_limit_seconds: z.number().default(0),        // server uses time_limit_seconds
  max_score: z.number().default(0),                 // server uses max_score
  options: z.union([z.string(), z.array(z.any())]).nullable().optional(), // Accept string or array
  orderingChoices: z.array(z.string()).optional().nullable(),
  orderingLabels: z.array(z.string()).optional().nullable()
});
export type ApiQuestion = z.infer<typeof ApiQuestionZ>;

/** Canonical UI shape used everywhere in React */
export const UiQuestionZ = z.object({
  id: z.number(),
  itemId: z.string(),
  text: z.string(),
  type: z.enum(["MCQ","LikertAgreement","Frequency","ORDERING","TIMED_NUMERIC","TEXT","Text"]).catch("TEXT"),
  dimensionTags: z.string().optional(),
  timeLimitSeconds: z.number(),
  options: z.array(z.object({ value: z.string(), label: z.string() })).default([]),
  orderingChoices: z.array(z.string()).optional(),
  orderingLabels: z.array(z.string()).optional()
});
export type UiQuestion = z.infer<typeof UiQuestionZ>;

/** Login & session */
export const StartSessionResponseZ = z.object({
  sessionId: z.string(),
  totalQuestions: z.number().default(0),
  resume: z.boolean().optional()
});
export type StartSessionResponse = z.infer<typeof StartSessionResponseZ>;

/** Answer submission payload - matches backend SubmitAnswerRequest exactly */
export const AnswerPayloadZ = z.object({
  ItemId: z.string(),                    // backend expects PascalCase ItemId
  Answer: z.string().optional(),         // backend expects PascalCase Answer  
  NumericAnswer: z.number().optional(),  // backend expects PascalCase NumericAnswer
  ResponseTimeMs: z.number().optional()  // backend expects PascalCase ResponseTimeMs
});
export type AnswerPayload = z.infer<typeof AnswerPayloadZ>;

/** Start session request body */
export const StartSessionRequestZ = z.object({
  NationalId: z.string()
});
export type StartSessionRequest = z.infer<typeof StartSessionRequestZ>;

/** Complete session response */
export const CompleteSessionResponseZ = z.object({
  message: z.string(),
  sessionId: z.string().optional(),
  resultId: z.string().optional()
});
export type CompleteSessionResponse = z.infer<typeof CompleteSessionResponseZ>;

/** Report status response */
export const ReportStatusResponseZ = z.object({
  isReady: z.boolean(),
  resultId: z.string().optional(),
  error: z.string().optional()
});
export type ReportStatusResponse = z.infer<typeof ReportStatusResponseZ>;

/** Question option for UI */
export const QuestionOptionZ = z.object({
  value: z.string(),
  label: z.string()
});
export type QuestionOption = z.infer<typeof QuestionOptionZ>;