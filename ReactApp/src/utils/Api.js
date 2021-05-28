import React, { useState, useContext } from 'react';
import { useAuth } from 'src/utils/Authentication';
import axios from 'axios';
import PropTypes from 'prop-types';

const baseUrl = 'https://localhost:6001/api/'; // Gonna fix this soon

export const ApiContext = React.createContext();

export class ApiService {
  constructor(authService) {
    this.authService = authService;
  }

  getToken() {
    return this.authService.getAuthTokens().access_token;
  }

  getServerMessages = async () => {
    const result = await axios.get(`${baseUrl}ServerMessages`, {
      headers: {
        Authorization: `Bearer ${this.getToken()}`
      }
    });

    console.log(result.data);

    return result.data;
  }

  deleteServerMessage = async (id) => {
    const result = await axios.delete(`${baseUrl}ServerMessages/${id}`, {
      headers: {
        Authorization: `Bearer ${this.getToken()}`
      }
    });

    return result.data;
  }

  createServerMessage = async (name, content, channelID) => {
    const result = await axios.post(`${baseUrl}ServerMessages`, {
      name,
      content,
      channelID
    }, {
      headers: { Authorization: `Bearer ${this.getToken()}` }
    });

    return result.data;
  }

  editServerMessage = async (id, name, content) => {
    const result = await axios.put(`${baseUrl}ServerMessages/${id}`, {
      name,
      content
    }, {
      headers: { Authorization: `Bearer ${this.getToken()}` }
    });

    return result.data;
  }

  getDiscordChannels = async () => {
    const result = await axios.get(`${baseUrl}server/text-channels`, {
      headers: {
        Authorization: `Bearer ${this.getToken()}`
      }
    });

    console.log(result.data);

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
