import { NavLink } from 'react-router'
import Icon from './Icon.tsx'
import type { IconName } from './Icon.tsx'

const MODES: { to: string; icon: IconName; label: string }[] = [
  { to: '/', icon: 'sparkles', label: 'Czat AI' },
  { to: '/listings', icon: 'list', label: 'Lista' },
]

export default function ModeSwitch() {
  return (
    <nav className="mode-switch" aria-label="Tryb wyszukiwania">
      {MODES.map(({ to, icon, label }) => (
        <NavLink key={to} to={to} end className="mode-switch__link">
          <Icon name={icon} />
          <span className="mode-switch__label">{label}</span>
        </NavLink>
      ))}
    </nav>
  )
}
