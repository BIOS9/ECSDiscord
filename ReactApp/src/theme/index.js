import { createMuiTheme, colors } from '@material-ui/core';
import shadows from './shadows';
import typography from './typography';

const theme = createMuiTheme({
  palette: {
    background: {
      default: '#F4F6F8',
      paper: colors.common.white
    },
    primary: {
      contrastText: '#ffffff',
      main: '#0f5435'
    },
    text: {
      primary: '#000',
      secondary: '#888'
    }
  },
  shadows,
  typography
});

export default theme;
