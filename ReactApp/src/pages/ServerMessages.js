import React, { useEffect, useState } from 'react';
import { Helmet } from 'react-helmet';
import { Box, Container } from '@material-ui/core';
import { useApi } from 'src/utils/Api';
import {
  useAuth
} from 'src/utils/Authentication';
import MessageList from 'src/components/servermessages/MessageListResults';
import MessageListToolbar from 'src/components/servermessages/MessageListToolbar';
import MessageCreate from 'src/components/servermessages/MessageCreate';
import MessageEdit from 'src/components/servermessages/MessageEdit';

const ServerMessages = () => {
  const authService = useAuth();
  const apiService = useApi();
  const [serverMessages, setServerMessages] = useState([]);
  const [discordChannels, setDiscordChannels] = useState([]);
  const [editMode, setEditMode] = useState(false);
  const [createMode, setCreateMode] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState();

  useEffect(async () => {
    if (authService.isAuthenticated()) {
      setServerMessages(await apiService.getServerMessages());
      setDiscordChannels(await apiService.getDiscordChannels());
    }
  }, []);

  const deleteMessage = async (id) => {
    if (window.confirm('Are you sure you want to delete this message?')) { // Yep im using confirm in react.
      await apiService.deleteServerMessage(id);
      setServerMessages(await apiService.getServerMessages());
    }
  };

  const edit = (id) => {
    setSelectedMessage(serverMessages.filter((m) => m.id === id)[0]);
    setEditMode(true);
    setCreateMode(false);
  };

  const create = () => {
    setEditMode(false);
    setCreateMode(true);
  };

  const display = async () => {
    setEditMode(false);
    setCreateMode(false);
    setServerMessages(await apiService.getServerMessages());
  };

  const selectInterface = () => {
    if (editMode) {
      return (
        <Container maxWidth={false}>
          <MessageEdit apiService={apiService} display={display} selectedMessage={selectedMessage} />
        </Container>
      );
    }

    if (createMode) {
      return (
        <Container maxWidth={false}>
          <MessageCreate apiService={apiService} discordChannels={discordChannels} display={display} />
        </Container>
      );
    }

    return (
      <Container maxWidth={false}>
        <MessageListToolbar create={create} />
        <Box sx={{ pt: 3 }}>
          <MessageList messages={serverMessages} deleteMessage={deleteMessage} edit={edit} />
        </Box>
      </Container>
    );
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
        {selectInterface()}
      </Box>
    </span>
  );
};

export default ServerMessages;
