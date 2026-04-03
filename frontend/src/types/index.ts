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
  url: string;
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

// FORMS
export interface CreateApplicationForm {
  company: string;
  jobTitle: string;
  jobDescription: string;
  status: string;
  resumeId: string;
}

// export interface UpdateApplicationForm {
//   company: string;
//   jobTitle: string;
//   jobDescription: string;
//   status: string;
// }