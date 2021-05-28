import { useState, useEffect } from 'react';
import PerfectScrollbar from 'react-perfect-scrollbar';
import {
  Box,
  Button,
  TextField,
  Card
} from '@material-ui/core';
import PropTypes from 'prop-types';

const MessageEdit = ({
  apiService,
  display,
  selectedMessage,
  ...rest
}) => {
  const [name, setName] = useState();
  const [message, setMessage] = useState();

  const editMsg = async () => {
    if (!name) {
      alert('Name is missing.');
      return;
    }
    if (!message) {
      alert('Message is missing.');
      return;
    }

    await apiService.editServerMessage(selectedMessage.id, name, message);
    display();
  };

  useEffect(async () => {
    setName(selectedMessage.name);
    setMessage(selectedMessage.content);
  }, []);

  return (
    <Card {...rest}>
      <PerfectScrollbar>
        <Box sx={{ m: 3 }}>
          <TextField
            fullWidth
            label="Name"
            variant="standard"
            required
            onChange={(e) => { setName(e.target.value); }}
            value={name}
          />

          <TextField
            sx={{ mt: 3 }}
            fullWidth
            label="Message"
            variant="outlined"
            multiline
            required
            rowsMin={5}
            onChange={(e) => { setMessage(e.target.value); }}
            value={message}
          />
          <Box
            sx={{ mt: 3 }}
            display="flex"
            justifyContent="flex-end"
          >
            <Button
              sx={{ mr: 1 }}
              onClick={editMsg}
              variant="contained"
              color="primary"
            >
              Update
            </Button>
            <Button
              onClick={display}
              variant="contained"
              color="secondary"
            >
              Cancel
            </Button>
          </Box>
        </Box>
      </PerfectScrollbar>
    </Card>
  );
};

MessageEdit.propTypes = {
  apiService: PropTypes.object.isRequired,
  display: PropTypes.func.isRequired,
  selectedMessage: PropTypes.object.isRequired
};

export default MessageEdit;
