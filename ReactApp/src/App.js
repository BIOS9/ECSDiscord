import 'react-perfect-scrollbar/dist/css/styles.css';
import { useRoutes } from 'react-router-dom';
import { ThemeProvider } from '@material-ui/core';
import AuthProvider from 'src/utils/Authentication';
import GlobalStyles from 'src/components/GlobalStyles';
import 'src/mixins/chartjs';
import theme from 'src/theme';
import routes from 'src/routes';

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
      <ThemeProvider theme={theme}>
        <GlobalStyles />
        {routing}
      </ThemeProvider>
    </AuthProvider>
  );
};

export default App;
