import { Fragment } from 'react'

const BULLET = /^[-•]\s+/

interface Run {
  text: string
  bold: boolean
  italic: boolean
}

type Block = { kind: 'paragraph'; lines: Run[][] } | { kind: 'list'; items: Run[][] }

function parseRuns(line: string): Run[] {
  const runs: Run[] = []
  let bold = false
  let italic = false
  let text = ''
  const flush = () => {
    if (text) {
      runs.push({ text, bold, italic })
      text = ''
    }
  }

  for (let index = 0; index < line.length; index++) {
    const char = line[index]
    if (char === '\\' && index + 1 < line.length) {
      text += line[++index]
    } else if (char === '*' && line[index + 1] === '*') {
      flush()
      bold = !bold
      index++
    } else if (char === '*') {
      flush()
      italic = !italic
    } else {
      text += char
    }
  }
  flush()
  return runs
}

function parseDescription(source: string): Block[] {
  const blocks: Block[] = []
  let continuesParagraph = false

  for (const raw of source.split('\n')) {
    const line = raw.trim()
    const last = blocks.at(-1)
    if (!line) {
      continuesParagraph = false
    } else if (BULLET.test(line)) {
      const item = parseRuns(line.replace(BULLET, ''))
      if (last?.kind === 'list') {
        last.items.push(item)
      } else {
        blocks.push({ kind: 'list', items: [item] })
      }
      continuesParagraph = false
    } else if (continuesParagraph && last?.kind === 'paragraph') {
      last.lines.push(parseRuns(line))
    } else {
      blocks.push({ kind: 'paragraph', lines: [parseRuns(line)] })
      continuesParagraph = true
    }
  }
  return blocks
}

function Runs({ runs }: { runs: Run[] }) {
  return runs.map((run, index) => {
    let node = <>{run.text}</>
    if (run.italic) {
      node = <em>{node}</em>
    }
    if (run.bold) {
      node = <strong>{node}</strong>
    }
    return <Fragment key={index}>{node}</Fragment>
  })
}

export default function Description({ source }: { source: string }) {
  return (
    <div className="description">
      {parseDescription(source).map((block, index) =>
        block.kind === 'list' ? (
          <ul key={index}>
            {block.items.map((item, itemIndex) => (
              <li key={itemIndex}>
                <Runs runs={item} />
              </li>
            ))}
          </ul>
        ) : (
          <p key={index}>
            {block.lines.map((line, lineIndex) => (
              <Fragment key={lineIndex}>
                {lineIndex > 0 && <br />}
                <Runs runs={line} />
              </Fragment>
            ))}
          </p>
        ),
      )}
    </div>
  )
}
