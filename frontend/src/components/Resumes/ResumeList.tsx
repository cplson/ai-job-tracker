import { useEffect, useState } from 'react';
import api from '../../services/api';
import type { ResumeDto } from '../../types';

export default function ResumeList() {
  const [resumes, setResumes] = useState<ResumeDto[]>([]);

  useEffect(() => {
    async function fetchResumes() {
      try {
        const res = await api.get<ResumeDto[]>('/resumes/me');
        setResumes(res.data);
      } catch (err) {
        console.error(err);
      }
    }
    fetchResumes();
  }, []);

  return (
    <div>
      <h2>My Resumes</h2>
      <ul>
        {resumes.map(r => (
          <li key={r.id}>{r.fileName} (Uploaded: {new Date(r.uploadedAt).toLocaleString()})</li>
        ))}
      </ul>
    </div>
  );
}