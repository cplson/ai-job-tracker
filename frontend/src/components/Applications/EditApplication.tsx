import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import api from '../../services/api';
import axios from 'axios'
import BackButton from '../Common/BackButton';

interface UpdateApplicationDto {
  company?: string;
  jobTitle?: string;
  jobDescription?: string;
  status?: 'Draft' | 'Applied' | 'Interviewing' | 'Offered' | 'Rejected';
}

export default function EditApplication() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [form, setForm] = useState<UpdateApplicationDto>({
    company: '',
    jobTitle: '',
    jobDescription: '',
    status: 'Draft'
  });

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function fetchApplication() {
      try {
        const res = await api.get(`/applications/${id}`);
        setForm(res.data);
      } catch {
        setError('Failed to load application');
      } finally {
        setLoading(false);
      }
    }

    fetchApplication();
  }, [id]);

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload: UpdateApplicationDto = {};

    if (form.company) payload.company = form.company;
    if (form.jobTitle) payload.jobTitle = form.jobTitle;
    if (form.jobDescription) payload.jobDescription = form.jobDescription;
    if (form.status) payload.status = form.status;

    try {
      await api.put(`/applications/${id}`, payload);
      navigate(`/applications/${id}`, { state: { success: 'Application updated!' } });
    } catch (error: any) {
    if (axios.isAxiosError(error)) {
      console.error("Axios error:", error.response?.status, error.response?.data);
    } else {
      console.error("Unexpected error:", error);
    }
  }
  };

  if (loading) return <p>Loading...</p>;

  return (
    <div className="row justify-content-center">
      <div className="col-md-6">
        <BackButton />

        <div className="card shadow">
          <div className="card-body">
            <h3 className="mb-4">Edit Application</h3>

            {error && <div className="alert alert-danger">{error}</div>}

            <form onSubmit={handleSubmit}>
              <input
                className="form-control mb-3"
                name="company"
                value={form.company}
                onChange={handleChange}
              />

              <input
                className="form-control mb-3"
                name="jobTitle"
                value={form.jobTitle}
                onChange={handleChange}
              />

              <textarea
                className="form-control mb-3"
                name="jobDescription"
                rows={4}
                value={form.jobDescription}
                onChange={handleChange}
              />

              <select
                className="form-select mb-3"
                name="status"
                value={form.status}
                onChange={handleChange}
              >
                <option>Draft</option>
                <option>Applied</option>
                <option>Interview</option>
                <option>Rejected</option>
                <option>Offer</option>
              </select>

              <button className="btn btn-primary w-100">
                Save Changes
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}