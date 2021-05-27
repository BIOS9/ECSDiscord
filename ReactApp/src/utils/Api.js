import React, { useState, useContext } from 'react';
import { useAuth } from 'src/utils/Authentication';
import axios from 'axios';
import PropTypes from 'prop-types';

const baseUrl = 'https://localhost:6001/'; // Gonna fix this soon

export const ApiContext = React.createContext();

export class ApiService {
  constructor(authService) {
    this.authService = authService;
  }

  getToken() {
    return this.authService.getAuthTokens().access_token;
  }

  getServerMessages = async () => {
    const result = await axios.get(`${baseUrl}api/ServerMessages`, {
      headers: {
        Authorization: `Bearer ${this.getToken()}`
      }
    });

    console.log(result.data);

    return result.data;
  }

  deleteServerMessage = async (id) => {
    const result = await axios.delete(`${baseUrl}api/ServerMessages/${id}`, {
      headers: {
        Authorization: `Bearer ${this.getToken()}`
      }
    });

    return result.data;
  }
}

export const useApi = () => {
  const context = useContext(ApiContext);
  if (context === undefined) {
    throw new Error('useApi must be used within a ApiProvider');
  }
  return context.apiService;
};

const ApiProvider = (props) => {
  const { children } = props;
  const authService = useAuth();
  const [apiService] = useState(new ApiService(authService));

  return (
    <ApiContext.Provider value={{ apiService }}>
      {children}
    </ApiContext.Provider>
  );
};

ApiProvider.propTypes = {
  children: PropTypes.node.isRequired
};

export default ApiProvider;
