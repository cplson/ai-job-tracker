import axios from 'axios';

export function getApiErrorMessage(err: unknown, fallback: string): string {
  if (!axios.isAxiosError(err) || !err.response) {
    return fallback;
  }

  const data = err.response.data;

  if (typeof data === 'string' && data.trim()) {
    return data;
  }

  if (data && typeof data === 'object') {
    if ('title' in data && typeof data.title === 'string') {
      return data.title;
    }

    if ('errors' in data && data.errors && typeof data.errors === 'object') {
      const messages = Object.values(data.errors as Record<string, string[]>)
        .flat()
        .filter(Boolean);
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }
  }

  return fallback;
}
