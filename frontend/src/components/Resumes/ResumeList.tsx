import { useCallback, useEffect, useState } from "react";
import api from "../../services/api";
import DeleteButton from "../Common/DeleteButton";
import Pagination from "../Common/Pagination";
import { useLocation, useNavigate, Link } from "react-router-dom";
import type { PagedResultDto, ResumeDto } from "../../types";

const SEARCH_DEBOUNCE_MS = 300;
const PAGE_SIZE = 10;

export default function ResumeList() {
  const [resumes, setResumes] = useState<ResumeDto[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedQuery, setDebouncedQuery] = useState("");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const location = useLocation();
  const navigate = useNavigate();
  const [showSuccess, setShowSuccess] = useState<string | null>(null);

  useEffect(() => {
    const success = location.state?.success;
    if (success === "created") setShowSuccess("Resume uploaded successfully");
    else if (success === "deleted") setShowSuccess("Resume deleted successfully");

    if (success) {
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location, navigate]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  useEffect(() => {
    setPage(1);
  }, [debouncedQuery]);

  const fetchResumes = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.get<PagedResultDto<ResumeDto>>("/resumes/me", {
        params: {
          page,
          pageSize: PAGE_SIZE,
          search: debouncedQuery.trim() || undefined,
        },
      });
      setResumes(res.data.items);
      setTotalCount(res.data.totalCount);
      setTotalPages(res.data.totalPages);
      setError("");
    } catch (err) {
      console.error(err);
      setError("Failed to load resumes");
    } finally {
      setLoading(false);
    }
  }, [page, debouncedQuery]);

  useEffect(() => {
    fetchResumes();
  }, [fetchResumes]);

  const handleDownload = async (id: string, downloadName: string) => {
    try {
      const res = await api.get(`/resumes/${id}/download`, {
        responseType: "blob",
      });

      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");

      link.href = url;
      link.setAttribute("download", downloadName);
      document.body.appendChild(link);
      link.click();

      link.remove();
    } catch (err) {
      console.error(err);
    }
  };

  const hasSearch = debouncedQuery.trim().length > 0;

  return (
    <div className="row justify-content-center">
      <div className="col-lg-8">
      {showSuccess && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          {showSuccess}
          <button
            type="button"
            className="btn-close"
            onClick={() => setShowSuccess(null)}
          ></button>
        </div>
      )}
      <div className="d-flex justify-content-between mb-3">
        <h2>My Resumes</h2>
        <Link to="/resumes/upload" className="btn btn-primary">
          + Upload Resume
        </Link>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="mb-3">
        <label htmlFor="resume-search" className="form-label visually-hidden">
          Search resumes
        </label>
        <input
          id="resume-search"
          type="search"
          className="form-control"
          placeholder="Search by name..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
      </div>

      <div className="card">
        <div className="card-body">
          {loading ? (
            <p className="text-muted mb-0">Loading resumes...</p>
          ) : totalCount === 0 && !hasSearch ? (
            <p>No resumes uploaded yet.</p>
          ) : resumes.length === 0 ? (
            <p>No resumes match your search.</p>
          ) : (
            <>
              <table className="table table-striped mb-0">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Uploaded</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {resumes.map((r) => (
                    <tr key={r.id} className="align-middle">
                      <td>📄 {r.name}</td>
                      <td>{new Date(r.uploadedAt).toLocaleString()}</td>
                      <td className="d-flex gap-2">
                        <button
                          className="btn btn-outline-primary btn-sm"
                          onClick={() => handleDownload(r.id, r.name)}
                        >
                          Download
                        </button>
                        <DeleteButton
                          label="Delete"
                          fallbackPath="/resumes"
                          successState="deleted"
                          onDelete={async () => {
                            try {
                              await api.delete(`/resumes/${r.id}`);
                              setShowSuccess("Resume deleted successfully");
                              if (resumes.length === 1 && page > 1) {
                                setPage((p) => p - 1);
                              } else {
                                await fetchResumes();
                              }
                            } catch (err: unknown) {
                              console.log(err);
                            }
                          }}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <Pagination
                page={page}
                pageSize={PAGE_SIZE}
                totalCount={totalCount}
                totalPages={totalPages}
                onPageChange={setPage}
              />
            </>
          )}
        </div>
      </div>
      </div>
    </div>
  );
}
