import axios from 'axios';
import type { InternalAxiosRequestConfig } from 'axios';

const API_URL = 'http://localhost:5001/api'; // your backend URL

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

export const analyzeApplication = (id: string) => {
  return api.post(`/ai/analyze/${id}`);
};

export default api;