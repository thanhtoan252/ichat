/**
 * Mirrors the response envelope contracts of IChat.Api v1. ASP.NET Core serialises with
 * the camelCase policy and `JsonStringEnumConverter`, so enums arrive as their names.
 */

export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly offset: number;
  readonly limit: number;
  readonly totalCount: number;
  readonly hasMore: boolean;
}

/** RFC 7807 body returned by ProblemDetails and the validation problem helper. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}
