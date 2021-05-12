import React, { useEffect, useState, useContext } from 'react';
import { AuthContext, AuthService } from 'react-oauth2-pkce';
import PropTypes from 'prop-types';
import { useLocation, useNavigate } from 'react-router-dom';

const authedRoutes = [
  '/app/courses',
  '/app/users',
  '/app/verification',
  '/app/server-messages',
  '/app/settings'
];

const adminRoutes = [
  '/app/users',
  '/app/server-messages',
  '/app/settings'
];

const noAuthRedirect = '/app/home';

export const doesRequireAuth = (route) => authedRoutes.includes(route);

export const doesRequireAdmin = (route) => adminRoutes.includes(route);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within a AuthProvider');
  }
  return context.authService;
};

export const redirectNoAuth = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const authService = useAuth();

  if (!authService.isAuthenticated() && doesRequireAuth(location.pathname)) {
    navigate(noAuthRedirect);
  }
};

const AuthProvider = (props) => {
  const { authServiceOptions, children } = props;
  const [authService, setAuthService] = useState(new AuthService(authServiceOptions));

  useEffect(() => {
    authService.setRerenderCallback(() => {
      setAuthService(new AuthService(authServiceOptions));
    });
  });

  return (
    <AuthContext.Provider value={{ authService }}>
      {children}
    </AuthContext.Provider>
  );
};

AuthProvider.propTypes = {
  authServiceOptions: PropTypes.object.isRequired,
  children: PropTypes.node.isRequired
};

export default AuthProvider;
