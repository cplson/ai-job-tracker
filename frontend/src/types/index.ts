export interface UserLoginDto {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  token: string;
}

export interface ResumeDto {
  id: string;
  fileName: string;
  uploadedAt: string;
}

export interface ApplicationDto {
  id: string;
  company: string;
  jobTitle: string;
  status: string;
  jobDescription?: string;
}