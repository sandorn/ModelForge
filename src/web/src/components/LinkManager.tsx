import { useEffect, useState } from 'react';
import { Table, TableHeader, TableRow, TableHeaderCell, TableBody, TableCell, Button, Text, Spinner, Badge } from '@fluentui/react-components';
import { apiClient } from '../services/apiClient';
import type { LinkMetadata, LinkRefreshResponse } from '../types/contracts';

const hostLabel = (h: number) => h === 1 ? 'Excel' : h === 2 ? 'PPT' : h === 3 ? 'Word' : '?';
const sourceLabel = (t: number) => t === 0 ? 'Range' : t === 1 ? 'Chart' : 'Pivot';

export function LinkManager() {
  const [links, setLinks] = useState<LinkMetadata[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState<Record<string, boolean>>({});
  const [error, setError] = useState<string>();

  useEffect(() => {
    loadLinks();
  }, []);

  async function loadLinks() {
    setLoading(true);
    setError(undefined);
    try {
      const data = await apiClient.getLinks();
      setLinks(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load links');
    } finally {
      setLoading(false);
    }
  }

  async function refreshLink(linkId: string) {
    setRefreshing(p => ({ ...p, [linkId]: true }));
    try {
      await apiClient.refreshLink(linkId, { linkId, requestedBy: 'anonymous' });
      await loadLinks();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Refresh failed');
    } finally {
      setRefreshing(p => ({ ...p, [linkId]: false }));
    }
  }

  if (loading) return <Spinner label="Loading links..." />;

  return (
    <div className="link-manager">
      <div className="link-manager-header">
        <Text size={500} weight="semibold">Link Manager</Text>
        <Button size="small" onClick={loadLinks}>Refresh</Button>
      </div>
      {error && <Text className="link-error">{error}</Text>}
      {links.length === 0 ? (
        <Text size={300}>No links found. Use Link to PowerPoint or Link to Word commands to create links.</Text>
      ) : (
        <Table size="small">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Source</TableHeaderCell>
              <TableHeaderCell>Target</TableHeaderCell>
              <TableHeaderCell>Policy</TableHeaderCell>
              <TableHeaderCell>Last Refresh</TableHeaderCell>
              <TableHeaderCell>Action</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {links.map(link => (
              <TableRow key={link.linkId}>
                <TableCell>
                  <Badge appearance="filled" color="brand">{hostLabel(link.sourceType)}</Badge>
                  {' '}{link.sourceAddress}
                </TableCell>
                <TableCell>
                  {link.targetAddress}
                </TableCell>
                <TableCell>{link.refreshPolicy}</TableCell>
                <TableCell>
                  {link.lastRefreshedAtUtc
                    ? new Date(link.lastRefreshedAtUtc).toLocaleString()
                    : 'Never'}
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    disabled={refreshing[link.linkId]}
                    onClick={() => refreshLink(link.linkId)}
                  >
                    {refreshing[link.linkId] ? 'Refreshing...' : 'Refresh'}
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
