import {
  Box,
  Button
} from '@material-ui/core';
import PropTypes from 'prop-types';

const MessageListToolbar = ({ create, ...rest }) => (
  <Box {...rest}>
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'flex-end'
      }}
    >
      <Button
        color="primary"
        variant="contained"
        onClick={create}
      >
        Create Message
      </Button>
    </Box>
  </Box>
);

MessageListToolbar.propTypes = {
  create: PropTypes.func.isRequired
};

export default MessageListToolbar;
