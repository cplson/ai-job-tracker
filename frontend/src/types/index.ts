export interface UserLoginDto {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  token: string;
}

export interface CreateUserDto {
  email: string;
  password: string;
}

export interface ReturnUserDto {
  id: string;
  email: string;
}

export interface RegisterResponseDto {
  token: string;
  returnedUser: ReturnUserDto;
}

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ResumeDto {
  id: string;
  name: string;
  uploadedAt: string;
}

export interface ApplicationDto {
  id: string;
  company: string;
  jobTitle: string;
  jobDescription?: string;
  status: string;
  resumeId?: string;
  resumeFileName?: string;
}

export interface AiAnalysisResultDto {
  summary: string;
  strengths: string[];
  weaknesses: string[];
  suggestions: string[];
  matchScore: number;
}

// FORMS
export interface CreateApplicationForm {
  company: string;
  jobTitle: string;
  jobDescription: string;
  status: string;
  resumeId?: string;
}

// export interface UpdateApplicationForm {
//   company: string;
//   jobTitle: string;
//   jobDescription: string;
//   status: string;
// }