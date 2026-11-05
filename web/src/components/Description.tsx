const BULLET = /^[-•]\s+/
const MAX_HEADING_LENGTH = 40

type Block = { kind: 'heading' | 'paragraph'; text: string } | { kind: 'list'; items: string[] }

function parseDescription(text: string): Block[] {
  const blocks: Block[] = []
  let continuesParagraph = false

  for (const raw of text.split('\n')) {
    const line = raw.trim()
    const last = blocks.at(-1)
    if (!line) {
      continuesParagraph = false
    } else if (BULLET.test(line)) {
      const item = line.replace(BULLET, '')
      if (last?.kind === 'list') {
        last.items.push(item)
      } else {
        blocks.push({ kind: 'list', items: [item] })
      }
      continuesParagraph = false
    } else if (line.endsWith(':') && line.length <= MAX_HEADING_LENGTH) {
      blocks.push({ kind: 'heading', text: line.slice(0, -1) })
      continuesParagraph = false
    } else if (continuesParagraph && last?.kind === 'paragraph') {
      last.text += `\n${line}`
    } else {
      blocks.push({ kind: 'paragraph', text: line })
      continuesParagraph = true
    }
  }
  return blocks
}

export default function Description({ text }: { text: string }) {
  return (
    <div className="description">
      {parseDescription(text).map((block, index) => {
        if (block.kind === 'heading') {
          return <h3 key={index}>{block.text}</h3>
        }
        if (block.kind === 'list') {
          return (
            <ul key={index}>
              {block.items.map((item, itemIndex) => (
                <li key={itemIndex}>{item}</li>
              ))}
            </ul>
          )
        }
        return <p key={index}>{block.text}</p>
      })}
    </div>
  )
}
