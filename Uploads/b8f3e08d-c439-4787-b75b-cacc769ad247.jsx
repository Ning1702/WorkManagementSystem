import { useEffect, useState } from 'react'
import { useAuth } from '../context/AuthContext'
import api from '../services/api'
import Navbar from '../components/Navbar'

export default function Tasks() {
  const [tasks, setTasks] = useState([])
  const [keyword, setKeyword] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ title: '', description: '', userIds: [], unitIds: [] })
  const [editId, setEditId] = useState(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const { role } = useAuth()
  const size = 10

  const fetchTasks = async () => {
    try {
      const res = await api.get('/tasks', {
        params: { keyword, status, page, size }
      })
      setTasks(res.data.data || [])
      setTotal(res.data.total || 0)
    } catch (err) {
      console.error(err)
    }
  }

  useEffect(() => {
    fetchTasks()
  }, [page, status])

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setSuccess('')
    try {
      if (editId) {
        await api.put(`/tasks/${editId}`, form)
        setSuccess('Cập nhật task thành công!')
      } else {
        await api.post('/tasks', form)
        setSuccess('Tạo task thành công!')
      }
      setForm({ title: '', description: '', userIds: [], unitIds: [] })
      setEditId(null)
      setShowForm(false)
      fetchTasks()
    } catch (err) {
      setError('Thao tác thất bại.')
    }
  }

  const handleEdit = (task) => {
    setEditId(task.id)
    setForm({ title: task.title, description: task.description || '', userIds: [], unitIds: [] })
    setShowForm(true)
  }

  const handleDelete = async (id) => {
    if (!window.confirm('Xóa task này?')) return
    try {
      await api.delete(`/tasks/${id}`)
      setSuccess('Đã xóa task!')
      fetchTasks()
    } catch {
      setError('Xóa thất bại.')
    }
  }

  const getStatusBadge = (status) => {
    const map = {
      Approved: 'bg-green-100 text-green-700',
      Rejected: 'bg-red-100 text-red-700',
      Submitted: 'bg-yellow-100 text-yellow-700',
      InProgress: 'bg-blue-100 text-blue-700',
      NotStarted: 'bg-gray-100 text-gray-700',
    }
    const label = {
      Approved: 'Đã phê duyệt',
      Rejected: 'Bị từ chối',
      Submitted: 'Đã nộp',
      InProgress: 'Đang thực hiện',
      NotStarted: 'Chưa bắt đầu',
    }
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-semibold ${map[status] || 'bg-gray-100 text-gray-600'}`}>
        {label[status] || status}
      </span>
    )
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <Navbar />
      <div className="p-6">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800">Danh sách công việc</h2>
          {role === 'Manager' && (
            <button
              onClick={() => { setShowForm(true); setEditId(null); setForm({ title: '', description: '', userIds: [], unitIds: [] }) }}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 font-semibold"
            >
              + Tạo task mới
            </button>
          )}
        </div>

        {error && <div className="bg-red-100 text-red-600 p-3 rounded mb-4 text-sm">{error}</div>}
        {success && <div className="bg-green-100 text-green-600 p-3 rounded mb-4 text-sm">{success}</div>}

        {/* Form tạo/sửa task */}
        {showForm && (
          <div className="bg-white rounded-xl shadow p-6 mb-6">
            <h3 className="text-lg font-semibold text-gray-700 mb-4">
              {editId ? 'Chỉnh sửa task' : 'Tạo task mới'}
            </h3>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tiêu đề</label>
                <input
                  type="text"
                  value={form.title}
                  onChange={(e) => setForm({ ...form, title: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Nhập tiêu đề task"
                  required
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Mô tả</label>
                <textarea
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  className="w-full border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  rows={3}
                  placeholder="Mô tả công việc..."
                />
              </div>
              <div className="flex gap-3">
                <button type="submit" className="bg-blue-600 text-white px-6 py-2 rounded-lg hover:bg-blue-700 font-semibold">
                  {editId ? 'Cập nhật' : 'Tạo mới'}
                </button>
                <button
                  type="button"
                  onClick={() => { setShowForm(false); setEditId(null) }}
                  className="bg-gray-200 text-gray-700 px-6 py-2 rounded-lg hover:bg-gray-300 font-semibold"
                >
                  Hủy
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Search + Filter */}
        <div className="flex gap-3 mb-4">
          <input
            type="text"
            placeholder="Tìm kiếm task..."
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
            className="border border-gray-300 rounded-lg px-4 py-2 w-64 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <select
            value={status}
            onChange={(e) => { setStatus(e.target.value); setPage(1) }}
            className="border border-gray-300 rounded-lg px-4 py-2 focus:outline-none"
          >
            <option value="">Tất cả trạng thái</option>
            <option value="NotStarted">Chưa bắt đầu</option>
            <option value="InProgress">Đang thực hiện</option>
            <option value="Submitted">Đã nộp báo cáo</option>
            <option value="Approved">Đã phê duyệt</option>
            <option value="Rejected">Bị từ chối</option>
          </select>
          <button
            onClick={() => { setPage(1); fetchTasks() }}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            Tìm kiếm
          </button>
        </div>

        {/* Task List */}
        <div className="bg-white rounded-xl shadow overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-blue-50 text-blue-700">
              <tr>
                <th className="px-4 py-3 text-left">Tiêu đề</th>
                <th className="px-4 py-3 text-left">Mô tả</th>
                <th className="px-4 py-3 text-left">Trạng thái</th>
                <th className="px-4 py-3 text-left">Ngày tạo</th>
                {role === 'Manager' && <th className="px-4 py-3 text-left">Thao tác</th>}
              </tr>
            </thead>
            <tbody>
              {tasks.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-6 text-gray-400">Không có task nào</td>
                </tr>
              ) : (
                tasks.map((task) => (
                  <tr key={task.id} className="border-t hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{task.title}</td>
                    <td className="px-4 py-3 text-gray-500">{task.description}</td>
                    <td className="px-4 py-3">{getStatusBadge(task.status)}</td>
                    <td className="px-4 py-3 text-gray-500">
                      {task.createdAt ? new Date(task.createdAt).toLocaleDateString('vi-VN') : '—'}
                    </td>
                    {role === 'Manager' && (
                      <td className="px-4 py-3">
                        <div className="flex gap-2">
                          <button onClick={() => handleEdit(task)} className="text-blue-600 hover:underline text-sm font-medium">Sửa</button>
                          <button onClick={() => handleDelete(task.id)} className="text-red-500 hover:underline text-sm font-medium">Xóa</button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex justify-center gap-2 mt-4">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-4 py-2 bg-white border rounded-lg hover:bg-gray-50 disabled:opacity-50"
          >
            Trước
          </button>
          <span className="px-4 py-2 text-gray-600">
            Trang {page} / {Math.ceil(total / size) || 1}
          </span>
          <button
            onClick={() => setPage(p => p + 1)}
            disabled={page >= Math.ceil(total / size)}
            className="px-4 py-2 bg-white border rounded-lg hover:bg-gray-50 disabled:opacity-50"
          >
            Tiếp
          </button>
        </div>
      </div>
    </div>
  )
}
