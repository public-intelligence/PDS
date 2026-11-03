export interface DisplayData {
  columnName: string;
  value: string;
  type: 'string' | 'date' | 'link' | 'boolean';
}

export interface ColumnData {
  id: string;
  name: string;
  columns: Column[];
  showActions?: boolean;
  actionsOptions?: Actions[];
  displayData?: DisplayData[];
  editPath?: string;
  state?: 'active' | 'inactive';
}

export interface Actions {
  name: string;
  color?: 'red' | 'green' | 'orange';
  icon?: string;
  function: (id: string) => void;
}

export interface Column {
  value: string;
  justify?: 'left' | 'center' | 'right';
  color?: 'red' | 'green' | 'yellow' | 'default';
  dataType: 'string' | 'number' | 'date' | 'grade' | 'boolean' | 'link';
  href?: string;
  target?: '_self' | '_blank';
}
