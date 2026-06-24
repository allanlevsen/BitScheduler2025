export function extractApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (typeof error === 'object' && error !== null) {
    const errorRecord = error as Record<string, unknown>;
    const payload = errorRecord['error'];

    if (typeof payload === 'string' && payload.trim().length > 0) {
      return payload;
    }

    if (typeof payload === 'object' && payload !== null) {
      const payloadRecord = payload as Record<string, unknown>;
      const detail = payloadRecord['detail'];
      const title = payloadRecord['title'];

      if (typeof detail === 'string' && detail.trim().length > 0) {
        return detail;
      }

      if (typeof title === 'string' && title.trim().length > 0) {
        return title;
      }
    }

    if (errorRecord['status'] === 409) {
      return 'The event conflicts with an existing reservation for the selected resource and time.';
    }
  }

  return fallbackMessage;
}
