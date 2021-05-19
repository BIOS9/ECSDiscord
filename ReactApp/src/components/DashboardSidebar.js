import React, { useEffect } from 'react';
import {
  useAuth,
  useUser,
  doesRequireAuth,
  doesRequireAdmin
} from 'src/utils/Authentication';
import { useApi } from 'src/utils/Api';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import PropTypes from 'prop-types';
import {
  Avatar,
  Box,
  Divider,
  Drawer,
  Hidden,
  List,
  Typography
} from '@material-ui/core';
import {
  BookOpen as BookIcon,
  MessageSquare as MessageSquareIcon,
  Settings as SettingsIcon,
  UserCheck as UserCheckIcon,
  Users as UsersIcon,
  Home as HomeIcon,
  LogIn as LoginIcon,
  LogOut as LogoutIcon
} from 'react-feather';
import NavItem from './NavItem';

const items = [
  {
    href: '/app/home',
    icon: HomeIcon,
    title: 'Home'
  },
  {
    href: '/app/courses',
    icon: BookIcon,
    title: 'Courses'
  },
  {
    href: '/app/users',
    icon: UsersIcon,
    title: 'Users'
  },
  {
    href: '/app/verification',
    icon: UserCheckIcon,
    title: 'Verification'
  },
  {
    href: '/app/server-messages',
    icon: MessageSquareIcon,
    title: 'Server Messages'
  },
  {
    href: '/app/settings',
    icon: SettingsIcon,
    title: 'Settings'
  }
];

const DashboardSidebar = ({ onMobileClose, openMobile }) => {
  const location = useLocation();
  const authService = useAuth();
  const apiService = useApi();
  const user = useUser();

  const login = async () => {
    await authService.authorize();
  };

  const logout = async () => {
    await authService.logout();
  };

  const getFilteredItems = () => {
    const authed = authService.isAuthenticated();
    const admin = true;
    return items
      .filter((x) => authed || !doesRequireAuth(x.href))
      .filter((x) => admin || !doesRequireAdmin(x.href));
  };

  useEffect(() => {
    if (1 - 1 === 10) {
      apiService.test();
    }
  }, []);

  useEffect(() => {
    if (openMobile && onMobileClose) {
      onMobileClose();
    }
  }, [location.pathname]);

  const content = (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%'
      }}
    >
      {authService.isAuthenticated() && (
        <span>
          <Box
            sx={{
              alignItems: 'center',
              display: 'flex',
              flexDirection: 'column',
              p: 2
            }}
          >
            <Avatar
              component={RouterLink}
              src={user.avatar}
              sx={{
                cursor: 'pointer',
                width: 64,
                height: 64
              }}
              to="/app/account"
            />
            <Typography
              color="textPrimary"
              variant="h5"
            >
              {user.username}
            </Typography>
            <Typography
              color="textSecondary"
              variant="body2"
            >
              Administrator
            </Typography>
          </Box>
          <Divider />
        </span>
      )}
      <Box sx={{ p: 2 }}>
        <List>
          {getFilteredItems().map((item) => (
            <NavItem
              href={item.href}
              key={item.title}
              title={item.title}
              icon={item.icon}
            />
          ))}
          {authService.isAuthenticated() ? (
            <NavItem
              href="#"
              key="Logout"
              title="Logout"
              icon={LogoutIcon}
              onClick={logout}
            />
          ) : (
            <NavItem
              href=""
              key="Login"
              title="Login"
              icon={LoginIcon}
              onClick={login}
            />
          )}
        </List>
      </Box>
      <Box sx={{ flexGrow: 1 }} />
    </Box>
  );

  return (
    <span>
      <Hidden lgUp>
        <Drawer
          anchor="left"
          onClose={onMobileClose}
          open={openMobile}
          variant="temporary"
          PaperProps={{
            sx: {
              width: 256
            }
          }}
        >
          {content}
        </Drawer>
      </Hidden>
      <Hidden lgDown>
        <Drawer
          anchor="left"
          open
          variant="persistent"
          PaperProps={{
            sx: {
              width: 256,
              top: 64,
              height: 'calc(100% - 64px)'
            }
          }}
        >
          {content}
        </Drawer>
      </Hidden>
    </span>
  );
};

DashboardSidebar.propTypes = {
  onMobileClose: PropTypes.func,
  openMobile: PropTypes.bool
};

DashboardSidebar.defaultProps = {
  onMobileClose: () => { },
  openMobile: false
};

export default DashboardSidebar;
