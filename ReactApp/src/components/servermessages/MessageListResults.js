import { useState } from 'react';
import PropTypes from 'prop-types';
import moment from 'moment';
import PerfectScrollbar from 'react-perfect-scrollbar';
import {
  Avatar,
  Box,
  Card,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  Typography,
  Button
} from '@material-ui/core';

const MessageListsResults = ({ messages, deleteMessage, ...rest }) => {
  const [limit, setLimit] = useState(10);
  const [page, setPage] = useState(0);

  const handleLimitChange = (event) => {
    setLimit(event.target.value);
  };

  const handlePageChange = (event, newPage) => {
    setPage(newPage);
  };

  return (
    <Card {...rest}>
      <PerfectScrollbar>
        <Box sx={{ minWidth: 1050 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>
                  Subject
                </TableCell>
                <TableCell>
                  Channel
                </TableCell>
                <TableCell>
                  Created
                </TableCell>
                <TableCell>
                  Last Edit
                </TableCell>
                <TableCell>
                  Actions
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {messages.slice(0, limit).map((message) => (
                <TableRow
                  hover
                  key={message.id}
                >
                  <TableCell>
                    <Box
                      sx={{
                        alignItems: 'center',
                        display: 'flex'
                      }}
                    >
                      <Typography
                        color="textPrimary"
                        variant="body1"
                      >
                        {message.name}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography
                      color="textPrimary"
                      variant="body1"
                    >
                      {`#${message.channel.name}`}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Box
                      sx={{
                        alignItems: 'center',
                        display: 'flex'
                      }}
                    >
                      <Typography
                        color="textPrimary"
                        variant="body1"
                      >
                        {`${moment.unix(message.createdAt).fromNow()} by ${message.creator.username}`}
                      </Typography>
                      <Avatar
                        src={message.creator.avatar}
                        sx={{ ml: 1 }}
                      />
                    </Box>
                  </TableCell>
                  <TableCell>
                    {(message.editedAt !== message.createdAt)
                    && (
                    <Box
                      sx={{
                        alignItems: 'center',
                        display: 'flex'
                      }}
                    >
                      <Typography
                        color="textPrimary"
                        variant="body1"
                      >
                        {`${moment.unix(message.editedAt).fromNow()} by ${message.editor.username}`}
                      </Typography>
                      <Avatar
                        src={message.creator.avatar}
                        sx={{ ml: 1 }}
                      />
                    </Box>
                    )}
                  </TableCell>
                  <TableCell>
                    <Box
                      sx={{
                        alignItems: 'center',
                        display: 'flex'
                      }}
                    >
                      <Button color="primary">Edit</Button>
                      <Button color="secondary" onClick={() => deleteMessage(message.id)}>Delete</Button>
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      </PerfectScrollbar>
      <TablePagination
        component="div"
        count={messages.length}
        onPageChange={handlePageChange}
        onRowsPerPageChange={handleLimitChange}
        page={page}
        rowsPerPage={limit}
        rowsPerPageOptions={[5, 10, 25]}
      />
    </Card>
  );
};

MessageListsResults.propTypes = {
  messages: PropTypes.array.isRequired,
  deleteMessage: PropTypes.func.isRequired
};

export default MessageListsResults;
