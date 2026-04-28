import { useMemo } from 'react';
import { Text } from '@mantine/core';
import { ReactFlow, ReactFlowProvider, Controls, MiniMap, Background } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { TopologyRelationship } from './types';

interface ClusterTopologyGraphProps {
  relationships: TopologyRelationship[];
  envId: string;
}

export function ClusterTopologyGraph({ relationships }: ClusterTopologyGraphProps) {
  const { nodes, edges } = useMemo(() => {
    const serverIds = new Set<string>();
    relationships.forEach(r => {
      serverIds.add(r.sourceNodeId);
      serverIds.add(r.targetNodeId);
    });

    const nodeList = Array.from(serverIds).map((id, i) => ({
      id,
      position: { x: (i % 4) * 220, y: Math.floor(i / 4) * 130 },
      data: { label: id },
      type: 'default' as const,
    }));

    const edgeList = relationships.map((r, i) => ({
      id: `e-${i}`,
      source: r.sourceNodeId,
      target: r.targetNodeId,
      label: r.safeLabel || r.type,
      animated: r.status === 'Unavailable',
    }));

    return { nodes: nodeList, edges: edgeList };
  }, [relationships]);

  if (relationships.length === 0) {
    return (
      <Text c="dimmed" ta="center" py="xl">
        No topology relationships available for this environment.
      </Text>
    );
  }

  return (
    <ReactFlowProvider>
      <div style={{ width: '100%', height: '500px' }}>
        <ReactFlow nodes={nodes} edges={edges} fitView>
          <Controls />
          <MiniMap />
          <Background />
        </ReactFlow>
      </div>
    </ReactFlowProvider>
  );
}
