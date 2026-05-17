import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../../services/api';
import type { CreateUserDto, RegisterResponseDto } from '../../types';

export default function Register() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    const dto: CreateUserDto = { email, password };

    try {
      const res = await api.post<RegisterResponseDto>('/users', dto);
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
        'status' in err.response &&
        err.response.status === 409
      ) {
        setError('An account with this email already exists.');
      } else {
        setError('Registration failed. Please check your details and try again.');
      }
    }
  };

  return (
    <div className="container mt-5" style={{ maxWidth: '400px' }}>
      <div className="card p-4 shadow">
        <h3 className="mb-3 text-center">Create Account</h3>

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
            />
          </div>

          <div className="mb-3">
            <input
              type="password"
              className="form-control"
              placeholder="Password (min. 6 characters)"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>

          <div className="mb-3">
            <input
              type="password"
              className="form-control"
              placeholder="Confirm password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              required
              minLength={6}
            />
          </div>

          <button className="btn btn-primary w-100 mb-3" type="submit">
            Register
          </button>
        </form>

        <p className="text-center mb-0">
          Already have an account?{' '}
          <Link to="/">Log in</Link>
        </p>
      </div>
    </div>
  );
}
