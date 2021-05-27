import {
  Box,
  Button
} from '@material-ui/core';

const MessageListToolbar = (props) => (
  <Box {...props}>
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'flex-end'
      }}
    >
      <Button
        color="primary"
        variant="contained"
      >
        Create Message
      </Button>
    </Box>
  </Box>
);

export default MessageListToolbar;
