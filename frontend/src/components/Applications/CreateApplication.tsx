// src/components/Applications/CreateApplication.tsx
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../services/api';

interface CreateApplicationDto {
  company: string;
  jobTitle: string;
  jobDescription: string;
}

export default function CreateApplication() {
  const navigate = useNavigate();

  const [form, setForm] = useState<CreateApplicationDto>({
    company: '',
    jobTitle: '',
    jobDescription: ''
  });

  const [error, setError] = useState('');

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setForm({
      ...form,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      await api.post('/applications', form);
      navigate('/applications');
    } catch (err) {
      console.error(err);
      setError('Failed to create application');
    }
  };

  return (
    <div className="row justify-content-center">
      <div className="col-md-6">
        <div className="card shadow">
          <div className="card-body">
            <h3 className="mb-4">New Application</h3>

            {error && <div className="alert alert-danger">{error}</div>}

            <form onSubmit={handleSubmit}>
              <div className="mb-3">
                <label className="form-label">Company</label>
                <input
                  type="text"
                  name="company"
                  className="form-control"
                  value={form.company}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Job Title</label>
                <input
                  type="text"
                  name="jobTitle"
                  className="form-control"
                  value={form.jobTitle}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="mb-3">
                <label className="form-label">Job Description</label>
                <textarea
                  name="jobDescription"
                  className="form-control"
                  rows={4}
                  value={form.jobDescription}
                  onChange={handleChange}
                />
              </div>

              <button className="btn btn-primary w-100" type="submit">
                Create Application
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}