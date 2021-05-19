import 'react-perfect-scrollbar/dist/css/styles.css';
import React from 'react';
import { useRoutes } from 'react-router-dom';
import { ThemeProvider } from '@material-ui/core';
import AuthProvider from './utils/Authentication';
import ApiProvider from './utils/Api';
import GlobalStyles from './components/GlobalStyles';
import './mixins/chartjs';
import theme from './theme';
import routes from './routes';

const authServiceOptions = {
  clientId: 'public-dashboard',
  location: window.location,
  provider: 'https://localhost:5001/connect',
  redirectUri: window.location.origin,
  scopes: ['openid', 'profile', 'ecsdiscord', 'offline_access'],
  autoRefresh: true,
  refreshSlack: -15
};

const App = () => {
  const routing = useRoutes(routes);

  return (
    <AuthProvider authServiceOptions={authServiceOptions}>
      <ApiProvider>
        <ThemeProvider theme={theme}>
          <GlobalStyles />
          {routing}
        </ThemeProvider>
      </ApiProvider>
    </AuthProvider>
  );
};

export default App;
