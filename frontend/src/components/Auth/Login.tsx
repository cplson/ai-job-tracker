import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../../services/api';
import type { UserLoginDto, LoginResponseDto } from '../../types';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    const dto: UserLoginDto = {
      email: email.trim().toLowerCase(),
      password,
    };
    try {
      const res = await api.post<LoginResponseDto>('/users/login', dto);
      localStorage.setItem('jwt', res.data.token);
      navigate('/applications');
    } catch (err: unknown) {
      console.error(err);
      if (
        typeof err === 'object' &&
        err !== null &&
        'response' in err &&
        typeof err.response === 'object' &&
        err.response !== null &&
        'status' in err.response
      ) {
        const status = err.response.status;
        if (status === 401) {
          setError('Invalid email or password.');
          return;
        }
        if (status === 400) {
          setError('Please enter your email and password.');
          return;
        }
      }
      setError('Login failed. Check that the API is running and try again.');
    }
  };

  return (
    <div className="container mt-5" style={{ maxWidth: '400px' }}>
      <div className="card p-4 shadow">
        <h3 className="mb-3 text-center">Login</h3>

        {error && <div className="alert alert-danger">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="mb-3">
            <input
              type="email"
              className="form-control"
              placeholder="Email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className="mb-3">
            <input
              type="password"
              className="form-control"
              placeholder="Password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>

          <button className="btn btn-primary w-100 mb-3" type="submit">
            Login
          </button>
        </form>

        <p className="text-center mb-0">
          Don&apos;t have an account?{' '}
          <Link to="/register">Register</Link>
        </p>
      </div>
    </div>
  );
}