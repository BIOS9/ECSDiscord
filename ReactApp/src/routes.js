import { Navigate } from 'react-router-dom';
import React from 'react';
import DashboardLayout from './components/DashboardLayout';
import MainLayout from './components/MainLayout';
import Account from './pages/Account';
import ServerMessages from './pages/ServerMessages';
import Dashboard from './pages/Dashboard';
import Home from './pages/Home';
import Login from './pages/Login';
import NotFound from './pages/NotFound';
import ProductList from './pages/ProductList';
import Register from './pages/Register';
import Settings from './pages/Settings';

const routes = [
  {
    path: 'app',
    element: <DashboardLayout />,
    children: [
      { path: 'home', element: <Home /> },
      { path: 'courses', element: <Dashboard /> },
      { path: 'users', element: <Account /> },
      { path: 'verification', element: <Account /> },
      { path: 'server-messages', element: <ServerMessages /> },
      { path: 'settings', element: <Settings /> },
      { path: 'products', element: <ProductList /> },
      { path: 'settings', element: <Settings /> },
      { path: '*', element: <NotFound /> }
    ]
  },
  {
    path: '/',
    element: <MainLayout />,
    children: [
      { path: 'login', element: <Login /> },
      { path: 'register', element: <Register /> },
      { path: '/', element: <Navigate to="/app/home" /> },
      { path: '*', element: <NotFound /> }
    ]
  }
];

export default routes;
