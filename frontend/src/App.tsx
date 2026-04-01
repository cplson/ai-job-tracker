import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './components/Auth/Login';
import ApplicationList from './components/Applications/ApplicationList';
import ResumeList from './components/Resumes/ResumeList';
import AppLayout from './components/Layout/AppLayout';
import CreateApplication from './components/Applications/CreateApplication';

const ProtectedRoute = ({ children }: { children: React.JSX.Element }) => {
  const token = localStorage.getItem('jwt');
return token ? children : <Navigate to="/" replace />;
};

function App() {
  return (
<Router>
      <Routes>
        {/* Public route */}
        <Route path="/" element={<Login />} />

        {/* Protected layout routes */}
        <Route
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route path="/applications" element={<ApplicationList />} />
          <Route path="/applications/new" element={<CreateApplication />} />
          <Route path="/resumes" element={<ResumeList />} />
        </Route>
      </Routes>
    </Router>
  );
}
export default App
