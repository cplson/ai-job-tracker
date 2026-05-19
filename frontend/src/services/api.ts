import axios from 'axios';
import type { InternalAxiosRequestConfig } from 'axios';

// Same path in dev and prod. Vite proxies /api → localhost:5001 in dev; nginx does in prod.
const API_URL = import.meta.env.VITE_API_URL ?? '/api';

const api = axios.create({
  baseURL: API_URL,
});

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (!config.headers){
    config.headers = config.headers ?? new axios.AxiosHeaders();
  }

  const token = localStorage.getItem('jwt')
  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`)
  }
  return config
})

export const getSavedAnalysis = (id: string) => {
  return api.get(`/ai/analyze/${id}`);
};

export const analyzeApplication = (id: string) => {
  return api.post(`/ai/analyze/${id}`);
};

export default api;