import PerfectScrollbar from 'react-perfect-scrollbar';
import { useState } from 'react';
import {
  Card,
  Button,
  TextField,
  Box,
  Autocomplete
} from '@material-ui/core';
import PropTypes from 'prop-types';

const MessageCreate = ({
  apiService,
  discordChannels,
  display,
  ...rest
}) => {
  const [name, setName] = useState();
  const [channel, setChannel] = useState();
  const [message, setMessage] = useState();

  const createMsg = async () => {
    if (!name) {
      alert('Name is missing.');
      return;
    }
    if (!channel) {
      alert('Channel is missing.');
      return;
    }
    if (!message) {
      alert('Message is missing.');
      return;
    }

    await apiService.createServerMessage(name, message, channel);
    display();
  };

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
          />

          <Autocomplete
            sx={{ mt: 3 }}
            options={discordChannels.sort((a, b) => -b.name.localeCompare(a.name))}
            getOptionLabel={(option) => option.name}
            renderInput={(params) => <TextField {...params} label="Channel" variant="outlined" />}
            onChange={(e, value) => { setChannel(value.id); }}
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
          />
          <Box
            sx={{ mt: 3 }}
            display="flex"
            justifyContent="flex-end"
          >
            <Button
              sx={{ mr: 1 }}
              onClick={createMsg}
              variant="contained"
              color="primary"
            >
              Create
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

MessageCreate.propTypes = {
  apiService: PropTypes.object.isRequired,
  discordChannels: PropTypes.array.isRequired,
  display: PropTypes.func.isRequired
};

export default MessageCreate;
