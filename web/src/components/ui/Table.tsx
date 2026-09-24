import type { ReactNode } from 'react'
import { EmptyState } from './EmptyState'

export interface TableColumn<T> {
  key: string
  header: string
  align?: 'left' | 'right'
  render: (row: T) => ReactNode
}

export interface TableProps<T> {
  columns: TableColumn<T>[]
  rows: T[]
  rowKey: (row: T) => string | number
  emptyTitle?: string
  emptyDescription?: string
}

/** design-system-spec.md "Incoterms table" pattern generalized: sticky header, row hover,
 * right-aligned tabular-nums for numeric columns (`.num`). Renders EmptyState when rows=[]. */
export function Table<T>({ columns, rows, rowKey, emptyTitle, emptyDescription }: TableProps<T>) {
  if (rows.length === 0) {
    return <EmptyState title={emptyTitle ?? 'No data'} description={emptyDescription} />
  }

  return (
    // A dense data table (route/mode/carrier/price/validity/status/actions) doesn't fit
    // 375px width — scope the horizontal scroll to the table itself instead of letting it
    // force the whole page wider (found during T15's responsive pass).
    <div className="overflow-x-auto">
      <table className="freight-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.key} className={column.align === 'right' ? 'num' : undefined} scope="col">
                {column.header || <span className="sr-only">Actions</span>}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={rowKey(row)}>
              {columns.map((column) => (
                <td key={column.key} className={column.align === 'right' ? 'num' : undefined}>
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
