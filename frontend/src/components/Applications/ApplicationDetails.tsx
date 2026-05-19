import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../../services/api';
import type { AiAnalysisResultDto, ApplicationDto, ResumeDto } from '../../types';
import { analyzeApplication, getSavedAnalysis } from '../../services/api';
import BackButton from '../Common/BackButton';
import DeleteButton from '../Common/DeleteButton';
import EditButton from '../Common/EditButton';

export default function ApplicationDetails() {
  const { id } = useParams();
  const [application, setApplication] = useState<ApplicationDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [resume, setResume] = useState<ResumeDto | null>(null);
  const [aiResult, setAiResult] = useState<AiAnalysisResultDto | null>(null);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState('');

  // TO DO - LOW PRIORITY: update backend get application to return the 
  //                resume with it, eliminating the need for an extra api request
  useEffect(() => {
    async function fetchApplication() {
      try {
        const appRes = await api.get<ApplicationDto>(`/applications/${id}`);
        setApplication(appRes.data);

        if (appRes.data.resumeId) {
          const res = await api.get<ResumeDto>(
            `/resumes/${appRes.data.resumeId}`
          );
          setResume(res.data);
        }

        try {
          const analysisRes = await getSavedAnalysis(id!);
          setAiResult(analysisRes.data);
        } catch {
          // No saved analysis yet
        }
      } catch (err) {
        console.error(err);
        setError('Failed to load application');
      } finally {
        setLoading(false);
      }

    }
    fetchApplication();
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!application) return <p>Application not found</p>;

  const handleDownloadResume = async () => {
    if (!resume) return;
    try {
      const res = await api.get(`/resumes/${resume.id}/download`, {
        responseType: "blob",
      });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", resume.name);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      console.error(err);
    }
  };

  const handleAnalyze = async () => {
    setAiLoading(true);
    setAiError('');

    try {
      const res = await analyzeApplication(application.id);
      setAiResult(res.data);
    } catch (err) {
      console.error(err);
      setAiError("Failed to analyze application");
    } finally {
      setAiLoading(false);
    }
  };

  return (
    <div className="row justify-content-center">
        <div className="col-lg-8">
            <div className="card shadow mb-4">
                <div className="d-flex justify-content-end gap-2 m-3">
                <EditButton to={`/applications/${application.id}/edit`} />
                <DeleteButton
                    fallbackPath="/applications"
                    successState="deleted"
                    onDelete={async () => {
                    await api.delete(`/applications/${application.id}`);
                    }}
                />
            </div>
            <div className="card-body">
                <h3 className="mb-3">{application.jobTitle}</h3>
                <h5 className="text-muted mb-3">{application.company}</h5>

                <span className="badge bg-primary mb-3">
                {application.status}
                </span>

                <hr />

                <h5>Job Description</h5>
                <p style={{ whiteSpace: 'pre-wrap' }}>
                {application.jobDescription || 'No description provided.'}
                </p>
                {resume && (
                  <div className="mb-3">
                    <h5>Resume</h5>
                    <p className="mb-2">📄 {resume.name}</p>
                    <button
                      type="button"
                      className="btn btn-outline-secondary"
                      onClick={handleDownloadResume}
                    >
                      Download Resume
                    </button>
                  </div>
                )}
              </div>
        </div>

        <div className="card shadow mb-4">
          <div className="card-body">
            <h5>AI Insights</h5>

            <button
              className="btn btn-primary mb-3"
              onClick={handleAnalyze}
              disabled={aiLoading}
            >
              {aiLoading
                ? "Analyzing..."
                : aiResult
                  ? "Re-analyze Application"
                  : "Analyze Application"}
            </button>

            {aiError && <div className="alert alert-danger">{aiError}</div>}

            {aiResult && (
              <>
                <section className="mb-4">
                  <h6 className="fw-semibold">Summary</h6>
                  <p className="mb-0">{aiResult.summary}</p>
                </section>

                <section className="mb-4">
                  <h6 className="fw-semibold">Strengths</h6>
                  <ul className="mb-0 ps-3">
                    {aiResult.strengths.map((item: string, idx: number) => (
                      <li key={idx}>{item}</li>
                    ))}
                  </ul>
                </section>

                <section className="mb-4">
                  <h6 className="fw-semibold">Weaknesses</h6>
                  <ul className="mb-0 ps-3">
                    {aiResult.weaknesses.map((item: string, idx: number) => (
                      <li key={idx}>{item}</li>
                    ))}
                  </ul>
                </section>

                <section className="mb-4">
                  <h6 className="fw-semibold">Suggestions</h6>
                  <ul className="mb-0 ps-3">
                    {aiResult.suggestions.map((item: string, idx: number) => (
                      <li key={idx}>{item}</li>
                    ))}
                  </ul>
                </section>

                <section className="mt-4">
                <h6 className="fw-semibold mb-2">Match Score</h6>
                <div className="progress">
                  <div
                    className="progress-bar"
                    role="progressbar"
                    style={{ width: `${aiResult.matchScore}%` }}
                    aria-valuenow={aiResult.matchScore}
                    aria-valuemin={0}
                    aria-valuemax={100}
                  >
                    {aiResult.matchScore}%
                  </div>
                </div>
              </section>
              </>
            )}

          </div>
        </div>

        <BackButton
          label="Back to Applications"
          fallbackPath="/applications"
          ignoreHistory
        />
      </div>
    </div>
  );
}