"use client"

import { useEditor, EditorContent } from "@tiptap/react"
import StarterKit from "@tiptap/starter-kit"
import Placeholder from "@tiptap/extension-placeholder"

import { cn } from "@/lib/utils"

interface RichTextEditorProps {
  value: string
  onChange: (html: string) => void
  placeholder?: string
  className?: string
}

export function RichTextEditor({
  value,
  onChange,
  placeholder = "Write something…",
  className,
}: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({ placeholder }),
    ],
    content: value || "<p></p>",
    onUpdate({ editor: e }) {
      onChange(e.getHTML())
    },
    immediatelyRender: false,
  })

  return (
    <div className={cn("flex flex-col rounded-md border border-input", className)}>
      <div
        className="flex flex-wrap items-center gap-0.5 border-b border-border bg-muted/40 p-1"
        role="toolbar"
        aria-label="Formatting options"
      >
        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleBold().run()}
          active={editor?.isActive("bold") ?? false}
          title="Bold"
        >
          <span className="font-bold">B</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleItalic().run()}
          active={editor?.isActive("italic") ?? false}
          title="Italic"
        >
          <span className="italic">I</span>
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleStrike().run()}
          active={editor?.isActive("strike") ?? false}
          title="Strikethrough"
        >
          <span className="line-through">S</span>
        </ToolbarButton>

        <div className="mx-1 h-4 w-px bg-border" aria-hidden />

        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleBulletList().run()}
          active={editor?.isActive("bulletList") ?? false}
          title="Bullet list"
        >
          ≡
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleOrderedList().run()}
          active={editor?.isActive("orderedList") ?? false}
          title="Numbered list"
        >
          1.
        </ToolbarButton>
        <ToolbarButton
          onClick={() => editor?.chain().focus().toggleBlockquote().run()}
          active={editor?.isActive("blockquote") ?? false}
          title="Blockquote"
        >
          "
        </ToolbarButton>
      </div>

      <EditorContent
        editor={editor}
        className={cn(
          "p-3 text-sm",
          "[&_.ProseMirror]:min-h-32 [&_.ProseMirror]:outline-none",
          "[&_.ProseMirror_p.is-editor-empty:first-child::before]:pointer-events-none",
          "[&_.ProseMirror_p.is-editor-empty:first-child::before]:float-left",
          "[&_.ProseMirror_p.is-editor-empty:first-child::before]:h-0",
          "[&_.ProseMirror_p.is-editor-empty:first-child::before]:text-muted-foreground",
          "[&_.ProseMirror_p.is-editor-empty:first-child::before]:content-[attr(data-placeholder)]",
        )}
      />
    </div>
  )
}

interface ToolbarButtonProps {
  onClick?: () => void
  active?: boolean
  title: string
  children: React.ReactNode
}

function ToolbarButton({
  onClick,
  active = false,
  title,
  children,
}: ToolbarButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={title}
      aria-pressed={active}
      className={cn(
        "min-w-7 rounded px-1.5 py-1 text-sm leading-none transition-colors",
        "hover:bg-muted",
        active && "bg-muted font-semibold",
      )}
    >
      {children}
    </button>
  )
}
