import React, { useEffect, useState } from 'react';
import { Helmet } from 'react-helmet';
import { Box, Container } from '@material-ui/core';
import { useApi } from 'src/utils/Api';
import {
  useAuth
} from 'src/utils/Authentication';
import MessageList from 'src/components/servermessages/MessageListResults';
import CustomerListToolbar from 'src/components/customer/CustomerListToolbar';

const ServerMessages = () => {
  const authService = useAuth();
  const apiService = useApi();
  const [serverMessages, setServerMessages] = useState([]);

  useEffect(async () => {
    if (authService.isAuthenticated()) {
      setServerMessages(await apiService.getServerMessages());
    }
  }, []);

  const deleteMessage = async (id) => {
    if (window.confirm('Are you sure you want to delete this message?')) { // Yep im using confirm in react.
      await apiService.deleteServerMessage(id);
      setServerMessages(await apiService.getServerMessages());
    }
  };

  return (
    <span>
      <Helmet>
        <title>Server Messages | WgtnBot</title>
      </Helmet>
      <Box
        sx={{
          backgroundColor: 'background.default',
          minHeight: '100%',
          py: 3
        }}
      >
        <Container maxWidth={false}>
          <CustomerListToolbar />
          <Box sx={{ pt: 3 }}>
            <MessageList messages={serverMessages} deleteMessage={deleteMessage} />
          </Box>
        </Container>
      </Box>
    </span>
  );
};

export default ServerMessages;
